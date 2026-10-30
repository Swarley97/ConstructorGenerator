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
            // Each annotated type is turned into its own symbol-free, equatable ConstructorInfo and stays a separate
            // pipeline item (no Collect()), so changing one type only re-emits that type's output.
            IncrementalValuesProvider<ConstructorInfo> constructorInfos = context.SyntaxProvider
                .CreateSyntaxProvider((node, _) => IsInterestingClass(node),
                                      (syntaxContext, cancellationToken) => GetConstructorInfo(syntaxContext, cancellationToken))
                .Where(x => x is not null)
                .Select((x, _) => x!);

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

            // a type can have several annotated nodes (partial declarations, multiple dependency members);
            // only the first one in declaration order builds the model so each type is generated exactly once
            SyntaxNode? canonicalNode = type.DeclaringSyntaxReferences
                .Select(x => x.GetSyntax(cancellationToken))
                .OfType<TypeDeclarationSyntax>()
                .SelectMany(x => new SyntaxNode[] { x }.Concat(x.Members))
                .FirstOrDefault(IsInterestingClass);

            if (canonicalNode != syntaxContext.Node)
            {
                return null;
            }

            return new ConstructorInfoBuilder().BuildFor(type);
        }
    }
}
