using System;
using System.Collections.Generic;
using System.Linq;
using ConstructorGenerator.Attributes;
using Microsoft.CodeAnalysis;

namespace ConstructorGenerator.Model;

internal class ConstructorInfoBuilder
{
    private List<INamedTypeSymbol> _typesToInspect = new();

    private readonly Dictionary<INamedTypeSymbol, ConstructorInfo> _constructorInfos =
        new(SymbolEqualityComparer.Default);

    public ICollection<ConstructorInfo> Build(IEnumerable<INamedTypeSymbol> classesToInspect)
    {
        _typesToInspect = classesToInspect.ToList();
        return _typesToInspect.Select(Build).ToList();
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
            return new ConstructorInfo(namedTypeSymbol,
                GetAccessibility(namedTypeSymbol),
                Array.Empty<ParameterInfo>(), baseParameters);
        }

        List<ParameterInfo> parameterInfos = new();
        foreach (ISymbol memberSymbol in namedTypeSymbol.GetMembers())
        {
            AttributeData? attributeData = 
                WellKnownAttributes.ConstructorDependencyAttribute.GetAttributeData(memberSymbol);
            if (attributeData == null)
                continue;

            TypedConstant isOptionalValue = attributeData.NamedArguments
                .FirstOrDefault(x => x.Key == nameof(ConstructorDependencyAttribute.IsOptional)).Value;
            bool isOptional = (bool)(isOptionalValue.Value ?? false);

            ParameterInfo? info = memberSymbol switch
            {
                IFieldSymbol { Type: INamedTypeSymbol fieldType } =>
                    new ParameterInfo(fieldType, null, memberSymbol.Name, isOptional),
                IPropertySymbol { Type: INamedTypeSymbol propertyType } =>
                    new ParameterInfo(propertyType, null, memberSymbol.Name, isOptional),
                _ => null
            };

            if (info == null)
                continue;
            
            parameterInfos.Add(info);
        }

        return new ConstructorInfo(namedTypeSymbol, GetAccessibility(namedTypeSymbol), parameterInfos, baseParameters);
    }

    private IReadOnlyCollection<ParameterInfo> GetBaseParametersIfAny(INamedTypeSymbol namedTypeSymbol)
    {
        INamedTypeSymbol? baseType = namedTypeSymbol.BaseType;
        if (baseType == null)
            return Array.Empty<ParameterInfo>();

        if (_typesToInspect.Contains(baseType))
        {
            if (_constructorInfos.TryGetValue(baseType, out ConstructorInfo info))
                return info.Parameters;

            info = Build(baseType);
            _constructorInfos[baseType] = info;
            return info.Parameters;
        }

        return GetParameterInfos(baseType);
    }

    private IReadOnlyCollection<ParameterInfo> GetParameterInfos(INamedTypeSymbol baseType)
    {
        IMethodSymbol? baseConstructor = baseType.Constructors.FirstOrDefault();
        if (baseConstructor == null)
            return Array.Empty<ParameterInfo>();
        return GetParameterInfos(baseConstructor);
    }

    private IReadOnlyCollection<ParameterInfo> GetParameterInfos(IMethodSymbol baseConstructor)
    {
        List<ParameterInfo> parameterInfos = new();
        foreach (IParameterSymbol parameter in baseConstructor.Parameters)
        {
            if (parameter.Type is not INamedTypeSymbol namedType)
                continue;

            parameterInfos.Add(new ParameterInfo(namedType, 
                parameter.Name, null, parameter.IsOptional));
        }

        return parameterInfos;
    }
}