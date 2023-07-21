using System;
using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConstructorGenerator.Model;
using Scriban;

namespace ConstructorGenerator.Generation;

internal class GenerateConstructorTask
{
    internal GenerationResult Generate(ConstructorInfo constructorInfo)
    {
        Template template = Template.Parse(CodeTemplates.ConstructorTemplate);
        GenerateParameterList parameterList = CreateParameterList(constructorInfo);

        Dictionary<string, object> scribanDataModel = new Dictionary<string, object>();
        GetNonParameterFields(scribanDataModel, constructorInfo);
        GetParameterFields(scribanDataModel, parameterList);

        return new GenerationResult(template.Render(scribanDataModel));
    }

    private void GetNonParameterFields(Dictionary<string, object> scribanDataModel, ConstructorInfo constructorInfo)
    {
        string namespaceFullName = constructorInfo.Type.ContainingNamespace.ToDisplayString();
        string className = constructorInfo.Type.Name;
        string accessModifier = constructorInfo.Type.DeclaredAccessibility.ToString().ToLowerInvariant();
        string typeArgs = constructorInfo.Type.IsGenericType
                            ? string.Join(", ", constructorInfo.Type.TypeParameters.Select(x => x.Name))
                            : string.Empty;
        string typeKeyword;
        if (constructorInfo.Type.TypeKind == TypeKind.Struct)
            typeKeyword = "struct";
        else if (constructorInfo.Type.IsRecord)
            typeKeyword = "record";
        else
            typeKeyword = "class";

        scribanDataModel.Add("Namespace", namespaceFullName);
        scribanDataModel.Add("ClassName", className);
        scribanDataModel.Add("TypeArguments", typeArgs);
        scribanDataModel.Add("ClassAccessModifier", accessModifier);
        scribanDataModel.Add("ConstructorAccessModifier", constructorInfo.ConstructorAccessibility.ToString().ToLower());
        scribanDataModel.Add("ClassType", typeKeyword);
    }

    private void GetParameterFields(Dictionary<string, object> scribanDataModel, GenerateParameterList parameterList)
    {
        string parameterListText = string.Join(", ", parameterList.AllParameters
            .Where(x => !x.Origin.IsInitialized)
            .OrderBy(x => x.IsOptional ? int.MaxValue : 0)
            .Select(x => $"{x.Type} {x.Name}{(x.IsOptional ? " = default" : string.Empty)}"));

        StringBuilder assignmentListBuilder = new();
        foreach (GenerateParameter? parameter in parameterList.Parameters)
        {
            if ((parameter.Origin.AssignmentTargetMemberName == null) || parameter.Origin.IsInitialized)
                continue;

            assignmentListBuilder.AppendLine($"{parameter.Origin.AssignmentTargetMemberName} = {parameter.Name};");
        }

        scribanDataModel.Add("ParameterList", parameterListText);
        scribanDataModel.Add("AssignmentList", assignmentListBuilder.ToString().TrimEnd());
        scribanDataModel.Add("HasBaseParameters", parameterList.BaseParameters.Count > 0);
        scribanDataModel.Add("BaseParameterList", string.Join(", ", parameterList.BaseParameters.Select(x => x.Name)));
    }

    private GenerateParameterList CreateParameterList(ConstructorInfo info)
    {
        List<GenerateParameter> parameters = new();
        List<GenerateParameter> baseParameter = new();

        static GenerateParameter Create(ParameterInfo parameter, int index)
        {
            string fullParameterType = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string? parameterName = parameter.AssignmentTargetMemberName ?? parameter.Name;
            if (parameterName != null)
            {
                if (parameterName.StartsWith("_", StringComparison.Ordinal))
                {
                    parameterName = parameterName.Substring(1);
                }
                parameterName = char.ToLower(parameterName[0]) + parameterName.Substring(1);
            }
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                parameterName = $"para{index}";
            }
            return new GenerateParameter(fullParameterType, parameterName!, parameter.IsOptional, parameter);
        }

        int index = 0;
        foreach (ParameterInfo? parameter in info.Parameters)
        {
            parameters.Add(Create(parameter, index));
            index++;
        }

        foreach (ParameterInfo? parameter in info.BaseParameters)
        {
            baseParameter.Add(Create(parameter, index));
            index++;
        }

        return new GenerateParameterList(parameters.Concat(baseParameter).ToList(),
                                         baseParameter, parameters);
    }

    private record GenerateParameterList(IReadOnlyList<GenerateParameter> AllParameters,
                                         IReadOnlyList<GenerateParameter> BaseParameters,
                                         IReadOnlyList<GenerateParameter> Parameters);

    private record GenerateParameter(string Type, string Name, bool IsOptional, ParameterInfo Origin);
}