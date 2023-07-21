using System.IO;
using System.Reflection;

namespace ConstructorGenerator
{
    internal static class CodeTemplates
    {
        private static string? _codeTemplate;

        internal static string ConstructorTemplate =>
            _codeTemplate ??= GetTemplate("ConstructorGenerator.Generation.GenerateConstructorTemplate.txt");

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
