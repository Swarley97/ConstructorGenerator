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
            // Each annotated declaration is turned into its own symbol-free, equatable ConstructorInfo. There is no
            // Collect() over the declarations combined with the whole compilation, so an unrelated change does not
            // force every model to be rebuilt as one unit; unchanged models compare equal and their output stays cached.
            IncrementalValuesProvider<ConstructorInfo> constructorInfos = context.SyntaxProvider
                .CreateSyntaxProvider((node, _) => IsInterestingClass(node),
                                      (syntaxContext, cancellationToken) => GetConstructorInfo(syntaxContext, cancellationToken))
                .Where(x => x is not null)
                .Select((x, _) => x!)
                // a type split into several annotated partial declarations yields the same model more than once
                .Collect()
                .SelectMany((infos, _) => infos.Distinct());

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

        private static ConstructorInfo? GetConstructorInfo(GeneratorSyntaxContext syntaxContext,
                                                           System.Threading.CancellationToken cancellationToken)
        {
            TypeDeclarationSyntax? declaration = syntaxContext.Node.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            if (declaration == null ||
                syntaxContext.SemanticModel.GetDeclaredSymbol(declaration, cancellationToken) is not INamedTypeSymbol type)
            {
                return null;
            }

            return new ConstructorInfoBuilder().BuildFor(type);
        }
    }
}
