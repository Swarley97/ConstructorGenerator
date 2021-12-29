using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ConstructorGenerator.Model;

namespace ConstructorGenerator.Generation;

internal class GenerateConstructorTask
{
    internal GenerationResult Generate(ConstructorInfo constructorInfo)
	{
		string template = CodeTemplates.ConstructorTemplate;
		template = ReplaceNonParameterFields(template, constructorInfo);

		var parameterList = CreateParameterList(constructorInfo);
		template = ReplaceParameterFields(template, parameterList);

		return new GenerationResult(template);
	}

	private string ReplaceNonParameterFields(string template, ConstructorInfo constructorInfo)
	{
		string namespaceFullName = constructorInfo.Type.ContainingNamespace.ToDisplayString();
		string className = constructorInfo.Type.Name;
		string accessModifier = constructorInfo.Type.DeclaredAccessibility.ToString().ToLowerInvariant();
		string typeArgs = constructorInfo.Type.IsGenericType
							? string.Join(", ", constructorInfo.Type.TypeParameters.Select(x => x.Name))
							: string.Empty;

		return template.Replace("$Namespace$", namespaceFullName)
					   .Replace("$Class_Name$", className)
					   .Replace("$Type_Arguments$", typeArgs)
					   .Replace("$Access_Modifier$", accessModifier);
	}
	private string ReplaceParameterFields(string template, GenerateParameterList parameterList)
	{
		string parameterListText = string.Join(",\n", parameterList.AllParameters.Select(x => $"{x.Type} {x.Name}"));
		template = template.Replace("$Parameter_List$", parameterListText);

		StringBuilder builder = new();
		foreach (var parameter in parameterList.Parameters)
		{
			if (parameter.Origin.AssignmentTargetMemberName == null)
				continue;

			builder.AppendLine($"{parameter.Origin.AssignmentTargetMemberName} = {parameter.Name};");
		}
		template = template.Replace("$Assignment_List$", builder.ToString());
		template = template.Replace("$Base_Parameter_List$", 
			string.Join(",\n", parameterList.BaseParameters.Select(x => x.Name)));
		
		return template;
	}

	private GenerateParameterList CreateParameterList(ConstructorInfo info)
	{
		List<GenerateParameter> parameters = new();
		List<GenerateParameter> baseParameter = new();

		static GenerateParameter Create(ParameterInfo parameter, int index)
		{
			string fullParameterType = parameter.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
			string parameterName = $"para{index}";

			return new GenerateParameter(fullParameterType, parameterName, parameter);
		}

		int index = 0;
		foreach (var parameter in info.Parameters)
		{
			parameters.Add(Create(parameter, index));
			index++;
		}

		foreach (var parameter in info.BaseParameters)
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

	private record GenerateParameter(string Type, string Name, ParameterInfo Origin);
}