using ConstructorGenerator.Generation;
using ConstructorGenerator.Model;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConstructorGenerator
{
    [Generator]
    public class ConstructorGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
            context.RegisterForSyntaxNotifications(() => new ConstructorDependencySyntaxReceiver());
            context.RegisterForPostInitialization(initializationContext => initializationContext.AddSource("ConstructorGenerator_Attributes.g.cs", CodeTemplates.AttributeTemplate));
        }
        
        public void Execute(GeneratorExecutionContext context)
        {
            if (context.SyntaxReceiver is not ConstructorDependencySyntaxReceiver attributeSyntaxReceiver)
                return;

            ClassDeclarationSyntax[] classesToInspect = attributeSyntaxReceiver.ClassesWithBaseConstructorAttribute
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