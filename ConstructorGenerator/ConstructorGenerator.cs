using ConstructorGenerator.Generation;
using ConstructorGenerator.Model;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using ConstructorGenerator.Attributes;

namespace ConstructorGenerator
{
    [Generator]
    public class ConstructorGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            IncrementalValuesProvider<INamedTypeSymbol?> interestingClasses =
                context.SyntaxProvider.CreateSyntaxProvider((node, _) => IsInterestingClass(node),
                                                            (syntaxContext, _) => GetNamedTypeSymbol(syntaxContext));

            IncrementalValuesProvider<ConstructorInfo> constructorInfos = interestingClasses.Collect().SelectMany((classes, _) =>
            {
                classes = classes.Distinct<INamedTypeSymbol?>(SymbolEqualityComparer.Default).ToImmutableArray();

                ConstructorInfoBuilder builder = new();
                return builder.Build(classes.Where(x => x is not null)!);
            });

            IncrementalValuesProvider<(ConstructorInfo, GenerationResult)> generationResult =
                constructorInfos.Select((x, _) => (x, new GenerateConstructorTask().Generate(x)));

            context.RegisterSourceOutput(generationResult, (sourceProductionContext, source) =>
            {
                sourceProductionContext.AddSource($"{source.Item1.Type.ContainingNamespace}.{source.Item1.Type.Name}_ConstructorGenerator.g.cs", source.Item2.Code);
            });
        }

        private static bool IsInterestingClass(SyntaxNode? syntaxNode)
        {
            return syntaxNode switch
            {
                ClassDeclarationSyntax classDeclaration when (WellKnownAttributes.GenerateBaseConstructorCallAttribute.IsDefined(classDeclaration.AttributeLists) || WellKnownAttributes.GenerateFullConstructorAttribute.IsDefined(classDeclaration.AttributeLists)) => true,
                _ => syntaxNode is MemberDeclarationSyntax
                {
                    Parent: TypeDeclarationSyntax
                } memberDeclarationSyntax && WellKnownAttributes.ConstructorDependencyAttribute.IsDefined(memberDeclarationSyntax.AttributeLists)
            };
        }

        private INamedTypeSymbol? GetNamedTypeSymbol(GeneratorSyntaxContext syntaxContext)
        {
            TypeDeclarationSyntax? typeDeclarationSyntax = syntaxContext.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (typeDeclarationSyntax == null)
            {
                return null;
            }
            return syntaxContext.SemanticModel.GetDeclaredSymbol(typeDeclarationSyntax) as INamedTypeSymbol;
        }
    }
}