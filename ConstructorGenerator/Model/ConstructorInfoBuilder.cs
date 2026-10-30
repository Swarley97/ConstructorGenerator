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

    private readonly Dictionary<INamedTypeSymbol, ConstructorInfo> _constructorInfos =
        new(SymbolEqualityComparer.Default);

    private readonly Dictionary<INamedTypeSymbol, List<ParameterModel>> _allParameterModels =
        new(SymbolEqualityComparer.Default);

    private readonly MemberValidator _memberValidator = new();

    /// <summary>
    /// Builds the model for a single annotated type. Annotated base types are resolved on demand, so the
    /// result depends only on the type itself and its base chain, not on the set of all annotated types.
    /// </summary>
    public ConstructorInfo BuildFor(INamedTypeSymbol namedTypeSymbol)
    {
        return Build(namedTypeSymbol);
    }

    /// <summary>
    /// Mirrors the syntax predicate of the generator: a source type with a generator attribute on itself or on one of its members.
    /// </summary>
    private static bool IsGenerationTarget(INamedTypeSymbol namedTypeSymbol)
    {
        if (namedTypeSymbol.DeclaringSyntaxReferences.Length == 0)
            return false;

        return WellKnownAttributes.GenerateFullConstructorAttribute.IsDefined(namedTypeSymbol) ||
               WellKnownAttributes.GenerateBaseConstructorCallAttribute.IsDefined(namedTypeSymbol) ||
               namedTypeSymbol.GetMembers().Any(WellKnownAttributes.ConstructorDependencyAttribute.IsDefined);
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

        ConstructorShape shape = CollectShape(namedTypeSymbol);

        List<DiagnosticInfo> diagnostics = new();
        ReportIfNotPartial(namedTypeSymbol, diagnostics);
        ReportIfStatic(namedTypeSymbol, diagnostics);
        ReportIfConstructorAlreadyExists(namedTypeSymbol, shape.Parameters.Concat(shape.InitializerParameters),
            diagnostics);

        ConstructorInfo constructorInfo = new(TypeModel.Create(namedTypeSymbol),
            GetAccessibility(namedTypeSymbol),
            shape.Parameters.Select(x => x.Info).ToEquatableArray(),
            shape.InitializerKind,
            shape.InitializerParameters.Select(x => x.Info).ToEquatableArray(),
            diagnostics.ToEquatableArray());

        _constructorInfos[namedTypeSymbol] = constructorInfo;
        _allParameterModels[namedTypeSymbol] = shape.Parameters.Concat(shape.InitializerParameters).ToList();
        return constructorInfo;
    }

    /// <summary>
    /// Collects the parameters of the constructor generated for a type. Works for constructed generic types
    /// as well, so the parameter types of a base such as <c>Base&lt;int&gt;</c> are the substituted ones.
    /// </summary>
    private ConstructorShape CollectShape(INamedTypeSymbol namedTypeSymbol)
    {
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

        return new ConstructorShape(initializerKind, initializerParameters, parameters);
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

    private static void ReportIfStatic(INamedTypeSymbol namedTypeSymbol, List<DiagnosticInfo> diagnostics)
    {
        if (!namedTypeSymbol.IsStatic)
            return;

        diagnostics.Add(DiagnosticInfo.Create(Diagnostics.StaticTypeNotSupported,
            namedTypeSymbol.Locations.FirstOrDefault() ?? Location.None, namedTypeSymbol.Name));
    }

    private static void ReportIfConstructorAlreadyExists(INamedTypeSymbol namedTypeSymbol,
                                                         IEnumerable<ParameterModel> generatedParameters,
                                                         List<DiagnosticInfo> diagnostics)
    {
        // the same ordering the emitted constructor uses
        ParameterModel[] orderedParameters = generatedParameters
            .OrderBy(x => x.Info.IsParams ? 2 : x.Info.IsOptional ? 1 : 0)
            .ToArray();

        IMethodSymbol? conflictingConstructor = namedTypeSymbol.InstanceConstructors.FirstOrDefault(constructor =>
            !constructor.IsImplicitlyDeclared &&
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

        // a base type is usually a constructed generic, the generated constructor belongs to its declaration
        INamedTypeSymbol originalBaseType = baseType.OriginalDefinition;
        if (!IsGenerationTarget(originalBaseType))
            return GetParameterInfos(baseType, namedTypeSymbol);

        ConstructorInfo baseInfo = Build(originalBaseType);
        if (baseInfo.Diagnostics.Count > 0 || !IsGeneratedConstructorChainable(baseInfo.ConstructorAccessibility))
        {
            // no callable constructor is generated for the base, so chain to one of its real constructors instead
            return GetParameterInfos(baseType, namedTypeSymbol);
        }

        if (SymbolEqualityComparer.Default.Equals(baseType, originalBaseType))
            return _allParameterModels[originalBaseType].ToList();

        ConstructorShape baseShape = CollectShape(baseType);
        return baseShape.Parameters.Concat(baseShape.InitializerParameters).ToList();
    }

    // generated base constructors live in the same assembly as the derived type
    private static bool IsGeneratedConstructorChainable(Accessibility accessibility)
    {
        return accessibility != Accessibility.Private && accessibility != Accessibility.NotApplicable;
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

    private sealed record ConstructorShape(ConstructorInitializerKind InitializerKind,
                                           List<ParameterModel> InitializerParameters,
                                           List<ParameterModel> Parameters);
}
