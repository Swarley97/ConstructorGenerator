using System;
using System.Collections.Generic;
using System.Linq;
using ConstructorGenerator.Attributes;
using ConstructorGenerator.Model.Helper;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConstructorGenerator.Model;

internal class ConstructorInfoBuilder
{
    private static readonly SymbolDisplayFormat TypeDisplayFormat = SymbolDisplayFormat.FullyQualifiedFormat
        .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    private List<INamedTypeSymbol> _typesToInspect = new();

    private readonly Dictionary<INamedTypeSymbol, ConstructorInfo> _constructorInfos =
        new(SymbolEqualityComparer.Default);

    private readonly Dictionary<INamedTypeSymbol, List<ParameterModel>> _allParameterModels =
        new(SymbolEqualityComparer.Default);

    private readonly MemberValidator _memberValidator = new();

    public IReadOnlyCollection<ConstructorInfo> Build(IEnumerable<INamedTypeSymbol> classesToInspect)
    {
        _typesToInspect = classesToInspect.OrderBy(GetTypeHierarchyDepth).ToList();

        return _typesToInspect.Select(Build).ToList();
    }

    private int GetTypeHierarchyDepth(INamedTypeSymbol namedTypeSymbol)
    {
        int depth = 0;
        INamedTypeSymbol? currentType = namedTypeSymbol;
        while (currentType != null)
        {
            depth++;
            currentType = currentType.BaseType;
        }
        return depth;
    }

    private Accessibility GetAccessibility(INamedTypeSymbol namedTypeSymbol)
    {
        Accessibility defaultAccessibility = namedTypeSymbol.IsAbstract ? Accessibility.Protected : Accessibility.Public;

        AttributeData? attributeData =
            WellKnownAttributes.GeneratedConstructorSettingsAttribute.GetAttributeData(namedTypeSymbol);
        if (attributeData == null)
            return defaultAccessibility;

        TypedConstant accessibilityTypeConstant = attributeData.NamedArguments.FirstOrDefault(x =>
            x.Key == nameof(GeneratedConstructorSettingsAttribute.ConstructorAccessibility)).Value;

        if (accessibilityTypeConstant.IsNull)
            return defaultAccessibility;

        ConstructorAccessibility constructorAccessibility = default;
        if (accessibilityTypeConstant.Value is int intValue &&
            Enum.IsDefined(typeof(ConstructorAccessibility), intValue))
        {
            constructorAccessibility = (ConstructorAccessibility)intValue;
        }

        return constructorAccessibility switch
        {
            ConstructorAccessibility.Default => defaultAccessibility,
            ConstructorAccessibility.SameAsClass => namedTypeSymbol.DeclaredAccessibility,
            ConstructorAccessibility.Private => Accessibility.Private,
            ConstructorAccessibility.Protected => Accessibility.Protected,
            ConstructorAccessibility.Internal => Accessibility.Internal,
            ConstructorAccessibility.Public => Accessibility.Public,
            _ => defaultAccessibility
        };
    }

    private ConstructorInfo Build(INamedTypeSymbol namedTypeSymbol)
    {
        if (_constructorInfos.TryGetValue(namedTypeSymbol, out ConstructorInfo existingInfo))
            return existingInfo;

        List<ParameterModel> initializerParameters;
        ConstructorInitializerKind initializerKind;

        IMethodSymbol? primaryConstructor = GetPrimaryConstructor(namedTypeSymbol);
        if (primaryConstructor != null)
        {
            // a non-primary constructor of a type with a primary constructor must chain to this(...)
            initializerKind = ConstructorInitializerKind.This;
            initializerParameters = GetParameterInfos(primaryConstructor);
        }
        else
        {
            initializerParameters = GetBaseParametersIfAny(namedTypeSymbol);
            initializerKind = initializerParameters.Count > 0
                ? ConstructorInitializerKind.Base
                : ConstructorInitializerKind.None;
        }

        List<ParameterModel> parameters = new();
        bool onlyForwardBaseParameters = initializerKind == ConstructorInitializerKind.Base &&
                                         WellKnownAttributes.GenerateBaseConstructorCallAttribute.IsDefined(namedTypeSymbol);
        if (!onlyForwardBaseParameters)
        {
            AttributeData? generateFullConstructorAttributeData =
                WellKnownAttributes.GenerateFullConstructorAttribute.GetAttributeData(namedTypeSymbol);

            foreach (ISymbol memberSymbol in namedTypeSymbol.GetMembers())
            {
                if (!_memberValidator.ShouldGenerateConstructorParameterForMember(memberSymbol,
                        generateFullConstructorAttributeData != null))
                    continue;

                ParameterModel? parameterInfo = GetParameterInfo(memberSymbol);
                if (parameterInfo != null)
                {
                    parameters.Add(parameterInfo);
                }
            }
        }

        List<DiagnosticInfo> diagnostics = new();
        ReportIfNotPartial(namedTypeSymbol, diagnostics);
        ReportIfConstructorAlreadyExists(namedTypeSymbol, primaryConstructor, parameters.Concat(initializerParameters),
            diagnostics);

        ConstructorInfo constructorInfo = new(TypeModel.Create(namedTypeSymbol),
            GetAccessibility(namedTypeSymbol),
            parameters.Select(x => x.Info).ToEquatableArray(),
            initializerKind,
            initializerParameters.Select(x => x.Info).ToEquatableArray(),
            diagnostics.ToEquatableArray());

        _constructorInfos[namedTypeSymbol] = constructorInfo;
        _allParameterModels[namedTypeSymbol] = parameters.Concat(initializerParameters).ToList();
        return constructorInfo;
    }

    private static IMethodSymbol? GetPrimaryConstructor(INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.InstanceConstructors.FirstOrDefault(x =>
            x.DeclaringSyntaxReferences.Any(reference => reference.GetSyntax() is TypeDeclarationSyntax));
    }

    private static void ReportIfNotPartial(INamedTypeSymbol namedTypeSymbol, List<DiagnosticInfo> diagnostics)
    {
        // the generated wrapper re-declares every containing type as partial, so all of them have to be partial
        for (INamedTypeSymbol? type = namedTypeSymbol; type != null; type = type.ContainingType)
        {
            TypeDeclarationSyntax? nonPartialDeclaration = type.DeclaringSyntaxReferences
                .Select(x => x.GetSyntax())
                .OfType<TypeDeclarationSyntax>()
                .FirstOrDefault(x => !x.Modifiers.Any(SyntaxKind.PartialKeyword));

            if (nonPartialDeclaration == null)
                continue;

            diagnostics.Add(DiagnosticInfo.Create(Diagnostics.TypeMustBePartial,
                nonPartialDeclaration.Identifier.GetLocation(), type.Name));
            return;
        }
    }

    private static void ReportIfConstructorAlreadyExists(INamedTypeSymbol namedTypeSymbol,
                                                         IMethodSymbol? primaryConstructor,
                                                         IEnumerable<ParameterModel> generatedParameters,
                                                         List<DiagnosticInfo> diagnostics)
    {
        // the same ordering the emitted constructor uses
        ParameterModel[] orderedParameters = generatedParameters
            .OrderBy(x => x.Info.IsParams ? 2 : x.Info.IsOptional ? 1 : 0)
            .ToArray();

        IMethodSymbol? conflictingConstructor = namedTypeSymbol.InstanceConstructors.FirstOrDefault(constructor =>
            !constructor.IsImplicitlyDeclared &&
            !SymbolEqualityComparer.Default.Equals(constructor, primaryConstructor) &&
            constructor.Parameters.Length == orderedParameters.Length &&
            constructor.Parameters.Select((x, i) => SymbolEqualityComparer.Default.Equals(x.Type, orderedParameters[i].TypeSymbol) &&
                                                    // ref, out and in cannot be overloaded on each other, only against by-value
                                                    (x.RefKind != RefKind.None) == (orderedParameters[i].Info.RefKind != RefKind.None))
                .All(x => x));

        if (conflictingConstructor == null)
            return;

        Location location = conflictingConstructor.Locations.FirstOrDefault() ??
                            namedTypeSymbol.Locations.FirstOrDefault() ??
                            Location.None;

        diagnostics.Add(DiagnosticInfo.Create(Diagnostics.ConstructorAlreadyExists, location, namedTypeSymbol.Name));
    }

    private ParameterModel? GetParameterInfo(ISymbol memberSymbol)
    {
        AttributeData? constructorDependencyAttributeData =
            WellKnownAttributes.ConstructorDependencyAttribute.GetAttributeData(memberSymbol);
        bool isOptional = ExtractIsOptional(constructorDependencyAttributeData);

        ITypeSymbol? memberType = memberSymbol switch
        {
            IFieldSymbol fieldSymbol => fieldSymbol.Type,
            IPropertySymbol propertySymbol => propertySymbol.Type,
            _ => null
        };

        return memberType == null
            ? null
            : CreateParameter(memberType, null, memberSymbol.Name, isOptional, RefKind.None, false);
    }

    private static bool ExtractIsOptional(AttributeData? attributeData)
    {
        bool isOptional = false;
        if (attributeData == null)
            return isOptional;
        TypedConstant isOptionalValue = attributeData.NamedArguments
            .FirstOrDefault(x => x.Key == nameof(ConstructorDependencyAttribute.IsOptional)).Value;
        isOptional = (bool)(isOptionalValue.Value ?? false);

        return isOptional;
    }

    private List<ParameterModel> GetBaseParametersIfAny(INamedTypeSymbol namedTypeSymbol)
    {
        INamedTypeSymbol? baseType = namedTypeSymbol.BaseType;
        if (baseType == null || baseType.SpecialType == SpecialType.System_Object)
            return new List<ParameterModel>();

        if (!_typesToInspect.Contains(baseType, SymbolEqualityComparer.Default))
            return GetParameterInfos(baseType, namedTypeSymbol);

        ConstructorInfo baseInfo = Build(baseType);
        if (baseInfo.Diagnostics.Count > 0)
        {
            // no constructor is generated for the base, so chain to one of its real constructors instead
            return GetParameterInfos(baseType, namedTypeSymbol);
        }

        return _allParameterModels[baseType].ToList();
    }

    private List<ParameterModel> GetParameterInfos(INamedTypeSymbol baseType, INamedTypeSymbol derivedType)
    {
        IMethodSymbol? baseConstructor = baseType.InstanceConstructors
            .Where(x => IsAccessibleFromDerivedType(x, derivedType))
            .OrderByDescending(x => x.Parameters.Length)
            .FirstOrDefault();

        return baseConstructor == null ? new List<ParameterModel>() : GetParameterInfos(baseConstructor);
    }

    private static bool IsAccessibleFromDerivedType(IMethodSymbol constructor, INamedTypeSymbol derivedType)
    {
        bool sameAssembly = SymbolEqualityComparer.Default.Equals(constructor.ContainingAssembly,
            derivedType.ContainingAssembly);

        return constructor.DeclaredAccessibility switch
        {
            Accessibility.Public => true,
            Accessibility.Protected => true,
            Accessibility.ProtectedOrInternal => true,
            Accessibility.Internal => sameAssembly,
            Accessibility.ProtectedAndInternal => sameAssembly,
            _ => false
        };
    }

    private List<ParameterModel> GetParameterInfos(IMethodSymbol baseConstructor)
    {
        List<ParameterModel> parameterInfos = new();
        foreach (IParameterSymbol parameter in baseConstructor.Parameters)
        {
            parameterInfos.Add(CreateParameter(parameter.Type, parameter.Name, null, parameter.IsOptional,
                parameter.RefKind, parameter.IsParams));
        }

        return parameterInfos;
    }

    private static ParameterModel CreateParameter(ITypeSymbol type,
                                                  string? name,
                                                  string? assignmentTargetMemberName,
                                                  bool isOptional,
                                                  RefKind refKind,
                                                  bool isParams)
    {
        string typeText = type.ToDisplayString(TypeDisplayFormat);
        bool requiresNullableContext = typeText != type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

        ParameterInfo info = new(typeText, name, assignmentTargetMemberName, isOptional, refKind, isParams,
            requiresNullableContext);

        return new ParameterModel(info, type);
    }

    /// <summary>
    /// Pairs the pipeline model with the originating type symbol, which is only needed while building
    /// (e.g. to detect conflicting user-declared constructors). The symbol never leaves the builder.
    /// </summary>
    private sealed record ParameterModel(ParameterInfo Info, ITypeSymbol TypeSymbol);
}
