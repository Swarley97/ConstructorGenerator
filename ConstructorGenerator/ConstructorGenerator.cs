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
            // Syntax nodes are reference-stable across runs as long as their tree is unchanged, whereas symbols
            // are recreated for every compilation. Keeping symbols out of the pipeline until the (equatable)
            // ConstructorInfo is produced is what allows the source output to be cached.
            IncrementalValuesProvider<TypeDeclarationSyntax> interestingClasses =
                context.SyntaxProvider.CreateSyntaxProvider((node, _) => IsInterestingClass(node),
                                                            (syntaxContext, _) => GetTypeDeclaration(syntaxContext))
                    .Where(x => x is not null)!;

            IncrementalValuesProvider<ConstructorInfo> constructorInfos = interestingClasses.Collect()
                .Combine(context.CompilationProvider)
                .SelectMany((input, cancellationToken) =>
                {
                    (ImmutableArray<TypeDeclarationSyntax> declarations, Compilation compilation) = input;

                    ImmutableArray<INamedTypeSymbol> classes = declarations
                        .Select(x => compilation.GetSemanticModel(x.SyntaxTree).GetDeclaredSymbol(x, cancellationToken))
                        .OfType<INamedTypeSymbol>()
                        .Distinct<INamedTypeSymbol>(SymbolEqualityComparer.Default)
                        .ToImmutableArray();

                    ConstructorInfoBuilder builder = new();
                    return builder.Build(classes);
                });

            // ConstructorInfo is symbol-free and equatable, so everything from here on is cached
            // as long as the generated constructor does not change.
            context.RegisterSourceOutput(constructorInfos, (sourceProductionContext, constructorInfo) =>
            {
                foreach (DiagnosticInfo diagnostic in constructorInfo.Diagnostics)
                {
                    sourceProductionContext.ReportDiagnostic(diagnostic.ToDiagnostic());
                }

                if (constructorInfo.Diagnostics.Count > 0)
                {
                    return;
                }

                GenerationResult result = new GenerateConstructorTask().Generate(constructorInfo);
                sourceProductionContext.AddSource(constructorInfo.Type.HintName, result.Code);
            });
        }

        private static bool IsInterestingClass(SyntaxNode? syntaxNode)
        {
            return syntaxNode switch
            {
                TypeDeclarationSyntax typeDeclaration when (WellKnownAttributes.GenerateBaseConstructorCallAttribute.IsDefined(typeDeclaration.AttributeLists) || WellKnownAttributes.GenerateFullConstructorAttribute.IsDefined(typeDeclaration.AttributeLists)) => true,
                _ => syntaxNode is MemberDeclarationSyntax
                {
                    Parent: TypeDeclarationSyntax
                } memberDeclarationSyntax && WellKnownAttributes.ConstructorDependencyAttribute.IsDefined(memberDeclarationSyntax.AttributeLists)
            };
        }

        private static TypeDeclarationSyntax? GetTypeDeclaration(GeneratorSyntaxContext syntaxContext)
        {
            return syntaxContext.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        }
    }
}
