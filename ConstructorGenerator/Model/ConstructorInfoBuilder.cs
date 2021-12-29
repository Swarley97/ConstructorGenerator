using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConstructorGenerator.Model;

internal class ConstructorInfoBuilder
{
    private readonly IReadOnlyCollection<ClassDeclarationSyntax> _classesToInspect;
    private readonly Compilation _compilation;

    private readonly List<INamedTypeSymbol> _typesToInspect = new();
    private readonly Dictionary<INamedTypeSymbol, ConstructorInfo> _constructorInfos = new();

    public ConstructorInfoBuilder(IReadOnlyCollection<ClassDeclarationSyntax> classesToInspect,
                                  Compilation compilation)
    {
        _classesToInspect = classesToInspect;
        _compilation = compilation;
    }

    public ICollection<ConstructorInfo> Build()
    {
        _typesToInspect.AddRange(_classesToInspect.Select(x => _compilation.GetSemanticModel(x.SyntaxTree)
                                                                           .GetDeclaredSymbol(x) as INamedTypeSymbol)
                                                  .Where(x => x != null)!);
        return _typesToInspect.Select(Build).ToList();
    }

    private ConstructorInfo Build(INamedTypeSymbol namedTypeSymbol)
    {
        IReadOnlyCollection<ParameterInfo> baseParameters = GetBaseParametersIfAny(namedTypeSymbol);

        if (namedTypeSymbol.GetAttributes()
            .Any(x => x.AttributeClass?.Name.StartsWith("GenerateConstructorBaseCall") is true &&
                      baseParameters.Count > 0))
        {
            return new ConstructorInfo(namedTypeSymbol, Array.Empty<ParameterInfo>(), baseParameters);
        }

        List<ParameterInfo> parameterInfos = new();
        foreach (ISymbol memberSymbol in namedTypeSymbol.GetMembers().Where(x => x.GetAttributes()
                     .Any(y => y.AttributeClass?.Name.StartsWith("ConstructorDependency") is true)))
        {
            switch (memberSymbol)
            {
                case IFieldSymbol { Type: INamedTypeSymbol fieldType }:
                    parameterInfos.Add(new ParameterInfo(fieldType, null, memberSymbol.Name));
                    break;
                case IPropertySymbol { Type: INamedTypeSymbol propertyType }:
                    parameterInfos.Add(new ParameterInfo(propertyType, null, memberSymbol.Name));
                    break;
            }
        }

        return new ConstructorInfo(namedTypeSymbol, parameterInfos, baseParameters);
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

            parameterInfos.Add(new ParameterInfo(namedType, parameter.Name, null));
        }

        return parameterInfos;
    }
}