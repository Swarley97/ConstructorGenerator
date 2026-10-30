using System;
using ConstructorGenerator.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace ConstructorGenerator.Generation;

internal class GenerateConstructorTask
{
    internal GenerationResult Generate(ConstructorInfo constructorInfo)
    {
        GenerateParameterList parameterList = CreateParameterList(constructorInfo);

        TypeDeclarationSyntax generatedType = CreateTypeDeclaration(constructorInfo.Type.Declaration)
            .AddMembers(CreateConstructor(constructorInfo, parameterList), CreateOnConstructingPartialMethod());

        MemberDeclarationSyntax rootType = WrapInContainingTypes(constructorInfo.Type, generatedType);

        CompilationUnitSyntax source = SyntaxFactory.CompilationUnit();
        if (constructorInfo.Type.Namespace == null)
        {
            source = source.AddMembers(rootType);
        }
        else
        {
            BaseNamespaceDeclarationSyntax namespaceDeclaration = SyntaxFactory.NamespaceDeclaration(
                    SyntaxFactory.ParseName(constructorInfo.Type.Namespace))
                .AddMembers(rootType);

            source = source.AddMembers(namespaceDeclaration);
        }

        source = source.NormalizeWhitespace();

        if (constructorInfo.RequiresNullableContext)
        {
            source = source.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia("#nullable enable" + Environment.NewLine));
        }

        return new GenerationResult(source.ToFullString());
    }

    private static TypeDeclarationSyntax CreateTypeDeclaration(TypeDeclarationModel type)
    {
        string modifiers = type.Modifiers.Length == 0 ? string.Empty : $"{type.Modifiers} ";
        string constraints = type.Constraints.Length == 0 ? string.Empty : $" {type.Constraints}";
        string declaration =
            $"{type.Accessibility} {modifiers}partial {type.Kind} {EscapeIfKeyword(type.Name)}{type.TypeParameterSuffix}{constraints} {{ }}";

        return (TypeDeclarationSyntax)SyntaxFactory.ParseMemberDeclaration(declaration)!;
    }

    private static MemberDeclarationSyntax WrapInContainingTypes(TypeModel type,
                                                                  TypeDeclarationSyntax generatedType)
    {
        TypeDeclarationSyntax currentType = generatedType;

        // containing types are stored outermost first, so wrap from the innermost outwards
        foreach (TypeDeclarationModel containingType in type.ContainingTypes.Reverse())
        {
            currentType = CreateTypeDeclaration(containingType).AddMembers(currentType);
        }

        return currentType;
    }

    private static ConstructorDeclarationSyntax CreateConstructor(ConstructorInfo constructorInfo,
                                                                  GenerateParameterList parameterList)
    {
        ConstructorDeclarationSyntax constructorDeclaration = SyntaxFactory.ConstructorDeclaration(EscapeIfKeyword(constructorInfo.Type.Name))
            .AddModifiers(GetAccessibilityTokens(constructorInfo.ConstructorAccessibility).ToArray())
            .WithParameterList(CreateConstructorParameterList(parameterList))
            .WithBody(CreateConstructorBody(parameterList));

        if (constructorInfo.InitializerKind != ConstructorInitializerKind.None)
        {
            constructorDeclaration = constructorDeclaration.WithInitializer(
                CreateConstructorInitializer(constructorInfo.InitializerKind, parameterList));
        }
        else if (parameterList.AllParameters.Count > 0 &&
                 constructorInfo.Type.Declaration.Kind.EndsWith("struct", StringComparison.Ordinal))
        {
            // a struct constructor must assign every field; chaining to this() default-initializes the unselected ones.
            // Not possible for the parameterless constructor, which would call itself.
            constructorDeclaration = constructorDeclaration.WithInitializer(
                SyntaxFactory.ConstructorInitializer(SyntaxKind.ThisConstructorInitializer));
        }

        return constructorDeclaration;
    }

    private static ParameterListSyntax CreateConstructorParameterList(GenerateParameterList parameterList)
    {
        SeparatedSyntaxList<ParameterSyntax> parameters = SyntaxFactory.SeparatedList(
            parameterList.AllParameters
                .OrderBy(GetParameterOrder)
                .Select(CreateParameter));

        return SyntaxFactory.ParameterList(parameters);
    }

    /// <summary>
    /// Required parameters first, then optional ones, and a params array always last.
    /// </summary>
    private static int GetParameterOrder(GenerateParameter parameter)
    {
        if (parameter.Origin.IsParams)
            return 2;

        return parameter.IsOptional ? 1 : 0;
    }

    private static ParameterSyntax CreateParameter(GenerateParameter parameter)
    {
        ParameterSyntax parameterSyntax = SyntaxFactory.Parameter(SyntaxFactory.Identifier(parameter.Name))
            .WithType(SyntaxFactory.ParseTypeName(parameter.Type));

        SyntaxKind modifierKind = GetParameterModifierKind(parameter.Origin);
        if (modifierKind != SyntaxKind.None)
        {
            parameterSyntax = parameterSyntax.AddModifiers(SyntaxFactory.Token(modifierKind));
        }

        if (!parameter.IsOptional || parameter.Origin.RefKind != RefKind.None || parameter.Origin.IsParams)
        {
            return parameterSyntax;
        }

        EqualsValueClauseSyntax defaultValue = SyntaxFactory.EqualsValueClause(
            SyntaxFactory.LiteralExpression(SyntaxKind.DefaultLiteralExpression));

        return parameterSyntax.WithDefault(defaultValue);
    }

    private static SyntaxKind GetParameterModifierKind(ParameterInfo parameter)
    {
        if (parameter.IsParams)
            return SyntaxKind.ParamsKeyword;

        return parameter.RefKind switch
        {
            RefKind.Ref => SyntaxKind.RefKeyword,
            RefKind.Out => SyntaxKind.OutKeyword,
            RefKind.In => SyntaxKind.InKeyword,
            _ => SyntaxKind.None
        };
    }

    private static BlockSyntax CreateConstructorBody(GenerateParameterList parameterList)
    {
        List<StatementSyntax> statements = new();
        foreach (GenerateParameter parameter in parameterList.Parameters)
        {
            if (parameter.Origin.AssignmentTargetMemberName == null)
            {
                continue;
            }

            string memberName = parameter.Origin.AssignmentTargetMemberName;
            IdentifierNameSyntax memberIdentifier = SyntaxFactory.IdentifierName(EscapeIfKeyword(memberName));
            ExpressionSyntax target = memberName == parameter.Name.TrimStart('@')
                // the member has the same name as the parameter (e.g. a lowercase field); qualify to avoid self assignment
                ? SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ThisExpression(),
                    memberIdentifier)
                : memberIdentifier;

            statements.Add(SyntaxFactory.ExpressionStatement(
                SyntaxFactory.AssignmentExpression(
                    SyntaxKind.SimpleAssignmentExpression,
                    target,
                    SyntaxFactory.IdentifierName(parameter.Name))));
        }

        statements.Add(SyntaxFactory.ExpressionStatement(
            SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("OnConstructing"))));

        return SyntaxFactory.Block(statements);
    }

    private static ConstructorInitializerSyntax CreateConstructorInitializer(ConstructorInitializerKind kind,
                                                                             GenerateParameterList parameterList)
    {
        SeparatedSyntaxList<ArgumentSyntax> arguments = SyntaxFactory.SeparatedList(
            parameterList.InitializerParameters.OrderBy(GetParameterOrder).Select(CreateArgument));

        SyntaxKind initializerKind = kind == ConstructorInitializerKind.This
            ? SyntaxKind.ThisConstructorInitializer
            : SyntaxKind.BaseConstructorInitializer;

        return SyntaxFactory.ConstructorInitializer(initializerKind, SyntaxFactory.ArgumentList(arguments));
    }

    private static ArgumentSyntax CreateArgument(GenerateParameter parameter)
    {
        ArgumentSyntax argument = SyntaxFactory.Argument(SyntaxFactory.IdentifierName(parameter.Name));

        SyntaxKind refKindKeyword = parameter.Origin.RefKind switch
        {
            RefKind.Ref => SyntaxKind.RefKeyword,
            RefKind.Out => SyntaxKind.OutKeyword,
            RefKind.In => SyntaxKind.InKeyword,
            _ => SyntaxKind.None
        };

        return refKindKeyword == SyntaxKind.None
            ? argument
            : argument.WithRefKindKeyword(SyntaxFactory.Token(refKindKeyword));
    }

    private static MethodDeclarationSyntax CreateOnConstructingPartialMethod()
    {
        return SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
                "OnConstructing")
            .AddModifiers(SyntaxFactory.Token(SyntaxKind.PartialKeyword))
            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
    }

    private static ImmutableArray<SyntaxToken> GetAccessibilityTokens(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword)),
            Accessibility.Private => ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.PrivateKeyword)),
            Accessibility.Internal => ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.InternalKeyword)),
            Accessibility.Protected => ImmutableArray.Create(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)),
            Accessibility.ProtectedOrInternal => ImmutableArray.Create(
                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword),
                SyntaxFactory.Token(SyntaxKind.InternalKeyword)),
            Accessibility.ProtectedAndInternal => ImmutableArray.Create(
                SyntaxFactory.Token(SyntaxKind.PrivateKeyword),
                SyntaxFactory.Token(SyntaxKind.ProtectedKeyword)),
            _ => ImmutableArray<SyntaxToken>.Empty
        };
    }

    private GenerateParameterList CreateParameterList(ConstructorInfo info)
    {
        List<GenerateParameter> parameters = new();
        List<GenerateParameter> initializerParameters = new();

        static GenerateParameter Create(ParameterInfo parameter, int index)
        {
            string? parameterName = parameter.AssignmentTargetMemberName ?? parameter.Name;
            if (parameterName != null)
            {
                if (parameterName.StartsWith("_", StringComparison.Ordinal))
                {
                    parameterName = parameterName.Substring(1);
                }

                if (parameterName.Length > 0)
                {
                    parameterName = char.ToLower(parameterName[0]) + parameterName.Substring(1);
                }
            }
            if (string.IsNullOrWhiteSpace(parameterName))
            {
                parameterName = $"para{index}";
            }
            return new GenerateParameter(parameter.Type, parameterName!, parameter.IsOptional, parameter);
        }

        int index = 0;
        foreach (ParameterInfo parameter in info.Parameters)
        {
            parameters.Add(Create(parameter, index));
            index++;
        }

        foreach (ParameterInfo parameter in info.InitializerParameters)
        {
            initializerParameters.Add(Create(parameter, index));
            index++;
        }

        List<GenerateParameter> allParameters = parameters.Concat(initializerParameters).ToList();
        DeDuplicateNames(allParameters);
        EscapeKeywords(allParameters);
        return new GenerateParameterList(allParameters, initializerParameters, parameters);
    }

    private static void DeDuplicateNames(IReadOnlyCollection<GenerateParameter> allParameters)
    {
        HashSet<string> usedNames = new(StringComparer.Ordinal);

        foreach (GenerateParameter parameter in allParameters)
        {
            string baseName = parameter.Name;
            string workingName = baseName;
            int counter = 1;

            while (!usedNames.Add(workingName))
            {
                workingName = $"{baseName}{counter}";
                counter++;
            }

            parameter.Name = workingName;
        }
    }

    private static void EscapeKeywords(IReadOnlyCollection<GenerateParameter> allParameters)
    {
        foreach (GenerateParameter parameter in allParameters)
        {
            parameter.Name = EscapeIfKeyword(parameter.Name);
        }
    }

    private static string EscapeIfKeyword(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
    }

    private record GenerateParameterList(IReadOnlyList<GenerateParameter> AllParameters,
                                         IReadOnlyList<GenerateParameter> InitializerParameters,
                                         IReadOnlyList<GenerateParameter> Parameters);

    private record GenerateParameter(string Type, string Name, bool IsOptional, ParameterInfo Origin)
    {
        public string Name { get; set; } = Name;
    }
}
