using System;
using System.Collections.Generic;
using System.Linq;
using ConstructorGenerator.Attributes;
using ConstructorGenerator.Model.Helper;
using Microsoft.CodeAnalysis;

namespace ConstructorGenerator.Model;

internal class ConstructorInfoBuilder
{
    private List<INamedTypeSymbol> _typesToInspect = new();

    private readonly Dictionary<INamedTypeSymbol, ConstructorInfo> _constructorInfos =
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
        AttributeData? attributeData =
            WellKnownAttributes.GeneratedConstructorSettingsAttribute.GetAttributeData(namedTypeSymbol);
        if (attributeData == null)
            return Accessibility.Public;

        TypedConstant accessibilityTypeConstant = attributeData.NamedArguments.FirstOrDefault(x =>
            x.Key == nameof(GeneratedConstructorSettingsAttribute.ConstructorAccessibility)).Value;

        if (accessibilityTypeConstant.IsNull)
            return Accessibility.Public;

        ConstructorAccessibility constructorAccessibility = default;
        if (accessibilityTypeConstant.Value is int intValue &&
            Enum.IsDefined(typeof(ConstructorAccessibility), intValue))
        {
            constructorAccessibility = (ConstructorAccessibility)intValue;
        }

        return constructorAccessibility switch
        {
            ConstructorAccessibility.Default => Accessibility.Public,
            ConstructorAccessibility.SameAsClass => namedTypeSymbol.DeclaredAccessibility,
            ConstructorAccessibility.Private => Accessibility.Private,
            ConstructorAccessibility.Protected => Accessibility.Protected,
            ConstructorAccessibility.Internal => Accessibility.Internal,
            ConstructorAccessibility.Public => Accessibility.Public,
            _ => Accessibility.Public
        };
    }

    private ConstructorInfo Build(INamedTypeSymbol namedTypeSymbol)
    {
        IReadOnlyCollection<ParameterInfo> baseParameters = GetBaseParametersIfAny(namedTypeSymbol);
        if (WellKnownAttributes.GenerateBaseConstructorCallAttribute.IsDefined(namedTypeSymbol) &&
            baseParameters.Count > 0)
        {
            return new ConstructorInfo(namedTypeSymbol, GetAccessibility(namedTypeSymbol), Array.Empty<ParameterInfo>(),
                baseParameters);
        }

        List<ParameterInfo> parameterInfos = new();
        AttributeData? generateFullConstructorAttributeData =
            WellKnownAttributes.GenerateFullConstructorAttribute.GetAttributeData(namedTypeSymbol);

        foreach (ISymbol memberSymbol in namedTypeSymbol.GetMembers())
        {
            if (!_memberValidator.ShouldGenerateConstructorParameterForMember(memberSymbol,
                    generateFullConstructorAttributeData != null))
                continue;

            ParameterInfo? parameterInfo = GetParameterInfo(memberSymbol);
            if (parameterInfo != null)
            {
                parameterInfos.Add(parameterInfo);
            }
        }

        ConstructorInfo constructorInfo =
            new(namedTypeSymbol, GetAccessibility(namedTypeSymbol), parameterInfos, baseParameters);

        _constructorInfos[namedTypeSymbol] = constructorInfo;
        return constructorInfo;
    }

    private ParameterInfo? GetParameterInfo(ISymbol memberSymbol)
    {
        AttributeData? constructorDependencyAttributeData =
            WellKnownAttributes.ConstructorDependencyAttribute.GetAttributeData(memberSymbol);
        bool isOptional = ExtractIsOptional(constructorDependencyAttributeData);

        return memberSymbol switch
        {
            IFieldSymbol { Type: INamedTypeSymbol fieldType } => new ParameterInfo(fieldType, null, memberSymbol.Name,
                isOptional),
            IPropertySymbol { Type: INamedTypeSymbol propertyType } => new ParameterInfo(propertyType, null,
                memberSymbol.Name, isOptional),
            _ => null
        };
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


    private IReadOnlyCollection<ParameterInfo> GetBaseParametersIfAny(INamedTypeSymbol namedTypeSymbol)
    {
        INamedTypeSymbol? baseType = namedTypeSymbol.BaseType;
        if (baseType == null)
            return Array.Empty<ParameterInfo>();

        if (!_typesToInspect.Contains(baseType))
            return GetParameterInfos(baseType);

        if (_constructorInfos.TryGetValue(baseType, out ConstructorInfo info))
            return info.AllParameters;

        info = Build(baseType);
        _constructorInfos[baseType] = info;
        return info.AllParameters;
    }

    private IReadOnlyCollection<ParameterInfo> GetParameterInfos(INamedTypeSymbol baseType)
    {
        IMethodSymbol? baseConstructor = baseType.Constructors.Where(x => !x.IsStatic)
            .OrderByDescending(x => x.Parameters.Length).FirstOrDefault();
        
        return baseConstructor == null ? Array.Empty<ParameterInfo>() : GetParameterInfos(baseConstructor);
    }

    private IReadOnlyCollection<ParameterInfo> GetParameterInfos(IMethodSymbol baseConstructor)
    {
        List<ParameterInfo> parameterInfos = new();
        foreach (IParameterSymbol parameter in baseConstructor.Parameters)
        {
            if (parameter.Type is not INamedTypeSymbol namedType)
                continue;

            parameterInfos.Add(new ParameterInfo(namedType, parameter.Name, null, parameter.IsOptional));
        }

        return parameterInfos;
    }
}