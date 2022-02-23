using System.Collections.Generic;
using ConstructorGenerator.Generation;
using ConstructorGenerator.Model;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Diagnostics;
using System.Collections.Immutable;

namespace ConstructorGenerator
{
	[Generator]
	public class ConstructorGenerator : IIncrementalGenerator
	{
		public void Initialize(IncrementalGeneratorInitializationContext context)
		{
			Debugger.Launch();

			IncrementalValuesProvider<INamedTypeSymbol?> interestingClasses =
				context.SyntaxProvider.CreateSyntaxProvider((node, _) => IsInterestingClass(node),
															(context, _) => GetNamedTypeSymbol(context));

			IncrementalValuesProvider<ConstructorInfo> constructorInfos = interestingClasses
				.Collect().SelectMany((classes, _) =>
			{
				classes = classes.Distinct<INamedTypeSymbol?>(SymbolEqualityComparer.Default).ToImmutableArray();

				ConstructorInfoBuilder builder = new();
				return builder.Build(classes.Where(x => x is not null)!);
			});

			IncrementalValuesProvider<(ConstructorInfo, GenerationResult)> generationResult = 
				constructorInfos.Select((x, _) => (x, new GenerateConstructorTask().Generate(x)));

			context.RegisterSourceOutput(generationResult, (context, source) =>
			{
				context.AddSource($"{source.Item1.Type.Name}_ConstructorGenerator.g.cs", source.Item2.Code);
			});
		}

		private bool IsInterestingClass(SyntaxNode? syntaxNode)
		{
			if (syntaxNode is ClassDeclarationSyntax classDeclaration)
			{
				if (classDeclaration.AttributeLists.SelectMany(x => x.Attributes)
					.All(x => x.Name.ToString() != "GenerateBaseConstructorCall"))
					return false;

				return true;
			}
			else if (syntaxNode is MemberDeclarationSyntax memberDelcaration)
			{
				if (memberDelcaration.AttributeLists.SelectMany(x => x.Attributes)
					.All(x => x.Name.ToString() != "ConstructorDependency"))
					return false;

				if (memberDelcaration.Parent is ClassDeclarationSyntax parentClassDeclaration)
					return true;
			}
			return false;
		}

		private INamedTypeSymbol? GetNamedTypeSymbol(GeneratorSyntaxContext syntaxContext)
		{
			ClassDeclarationSyntax? classDeclaration = syntaxContext.Node.FirstAncestorOrSelf<ClassDeclarationSyntax>();
			if (classDeclaration == null)
			{
				return null;
			}
			return syntaxContext.SemanticModel.GetDeclaredSymbol(classDeclaration) as INamedTypeSymbol;
		}
	}
}