using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace ConstructorGenerator
{
	internal static class CodeTemplates
	{
		private static string? _codeTemplate;
		private static string? _attributeTemplate;

		internal static string ConstructorTemplate => 
			_codeTemplate ??= GetTemplate("ConstructorGenerator.Generation.GenerateConstructorTemplate.txt");

		internal static string AttributeTemplate =>
			_attributeTemplate ??= GetTemplate("ConstructorGenerator.Attributes.txt");

		private static string GetTemplate(string templatePath)
		{
			using Stream? stream = Assembly.GetExecutingAssembly()
					.GetManifestResourceStream(templatePath);

			if (stream == null)
				throw new FileNotFoundException(templatePath);
			
			using StreamReader reader = new(stream);
			return reader.ReadToEnd();
		}
	}
}
