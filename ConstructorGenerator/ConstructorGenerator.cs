using ConstructorGenerator.Generation;
using ConstructorGenerator.Model;
using Microsoft.CodeAnalysis;
using System.Linq;

namespace ConstructorGenerator
{
    [Generator]
    public class ConstructorGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ConstructorDependencySyntaxReceiver());
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not ConstructorDependencySyntaxReceiver attributeSyntaxReceiver)
                return;

            context.AddSource("ConstructorGenerator_Attributes.g.cs", CodeTemplates.AttributeTemplate);

            var classesToInspect = attributeSyntaxReceiver.ClassesWithBaseConstructorAttribute
                .Concat(attributeSyntaxReceiver.ClassesWithConstructorDependencyAttributes)
                .ToArray();

            ConstructorInfoBuilder builder = new(classesToInspect, context.Compilation);
            var constructors = builder.Build();

            foreach (var info in constructors)
            {
                GenerateConstructorTask generateTask = new();
                var result = generateTask.Generate(info);

                context.AddSource($"ConstructorGenerator_{info.Type.Name}.g.cs", result.Code);
            }
        }
    }
}