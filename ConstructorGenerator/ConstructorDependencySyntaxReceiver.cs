using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConstructorGenerator
{
    public class ConstructorDependencySyntaxReceiver : ISyntaxReceiver
    {
        public HashSet<ClassDeclarationSyntax> ClassesWithConstructorDependencyAttributes { get; } = new();

        public HashSet<ClassDeclarationSyntax> ClassesWithBaseConstructorAttribute { get; } = new();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            if (syntaxNode is ClassDeclarationSyntax classDeclaration)
            {
                if (classDeclaration.AttributeLists.SelectMany(x => x.Attributes)
                    .All(x => x.Name.ToString() != "GenerateBaseConstructorCall"))
                    return;

                ClassesWithBaseConstructorAttribute.Add(classDeclaration);
            }
            else if (syntaxNode is MemberDeclarationSyntax memberDelcaration)
            {
                if (memberDelcaration.AttributeLists.SelectMany(x => x.Attributes)
                    .All(x => x.Name.ToString() != "ConstructorDependency"))
                    return;

                if (memberDelcaration.Parent is ClassDeclarationSyntax parentClassDeclaration)
                    ClassesWithConstructorDependencyAttributes.Add(parentClassDeclaration);
            }
        }
    }
}