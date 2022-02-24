using System;
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
		string typeKeyword;
		if (constructorInfo.Type.TypeKind == TypeKind.Struct)
			typeKeyword = "struct";
		else if (constructorInfo.Type.IsRecord)
			typeKeyword = "record";
		else
			typeKeyword = "class";
		
		return template.Replace("$Namespace$", namespaceFullName)
					   .Replace("$Class_Name$", className)
					   .Replace("$Type_Arguments$", typeArgs)
					   .Replace("$Access_Modifier$", accessModifier)
					   .Replace("$Constructor_Access_Modifier$", 
						   constructorInfo.ConstructorAccessibility.ToString().ToLower())
					   .Replace("$Type_Keyword$", typeKeyword);
	}
	
	private string ReplaceParameterFields(string template, GenerateParameterList parameterList)
	{
		string parameterListText = string.Join(", ", parameterList.AllParameters
			.OrderBy(x => x.IsOptional ? int.MaxValue : 0)
			.Select(x => $"{x.Type} {x.Name}{(x.IsOptional ? " = default" : string.Empty)}"));
		template = template.Replace("$Parameter_List$", parameterListText);

		int indexOfAssignmentList = template.IndexOf("$Assignment_List$", StringComparison.OrdinalIgnoreCase) - 1;
		StringBuilder prefixBuilder = new();
		for (int i = indexOfAssignmentList; i > 0; i--)
		{
			char current = template[i];
			if (current is '\n' or '\r')
				break;

			if (current == '\t' || char.IsWhiteSpace(current))
				prefixBuilder.Append(current);
		}

		string prefix = prefixBuilder.ToString();
		bool firstProcessed = false;
		StringBuilder builder = new();
		foreach (GenerateParameter? parameter in parameterList.Parameters)
		{
			if (parameter.Origin.AssignmentTargetMemberName == null)
				continue;

			builder.AppendLine($"{(firstProcessed ? prefix : string.Empty)}{parameter.Origin.AssignmentTargetMemberName} = {parameter.Name};");
			firstProcessed = true;
		}
		template = template.Replace("$Assignment_List$", builder.ToString());

		if (parameterList.BaseParameters.Count > 0)
		{
			string baseParameterList = string.Join(", ", parameterList.BaseParameters.Select(x => x.Name));
			template = template.Replace("$Base_Parameter_List$", baseParameterList);
		}
		else
		{
			template = template.Replace(": base($Base_Parameter_List$)", string.Empty);
		}
		
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

			return new GenerateParameter(fullParameterType, parameterName, parameter.IsOptional, parameter);
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

	private record GenerateParameter(string Type, string Name, bool IsOptional, ParameterInfo Origin);
}