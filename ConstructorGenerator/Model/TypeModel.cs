using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ConstructorGenerator.Model;

/// <summary>
/// Symbol-free description of a single (possibly containing) type declaration.
/// </summary>
internal record TypeDeclarationModel(string Name,
    string Kind,
    string Accessibility,
    string TypeParameterSuffix,
    string Modifiers,
    string Constraints)
{
    public static TypeDeclarationModel Create(INamedTypeSymbol type)
    {
        return new TypeDeclarationModel(type.Name,
            GetKindText(type),
            GetAccessibilityText(type.DeclaredAccessibility),
            GetTypeParameterSuffix(type),
            GetModifiers(type),
            GetConstraints(type));
    }

    private static string GetModifiers(INamedTypeSymbol type)
    {
        List<string> modifiers = new();
        // unsafe only applies to the declaration it is written on, so the generated part needs it too (pointer members)
        if (type.DeclaringSyntaxReferences
            .Select(x => x.GetSyntax())
            .OfType<Microsoft.CodeAnalysis.CSharp.Syntax.TypeDeclarationSyntax>()
            .Any(x => x.Modifiers.Any(SyntaxKind.UnsafeKeyword)))
        {
            modifiers.Add("unsafe");
        }

        if (type.IsReadOnly)
        {
            modifiers.Add("readonly");
        }

        if (type.IsRefLikeType)
        {
            modifiers.Add("ref");
        }

        return string.Join(" ", modifiers);
    }

    private static string GetConstraints(INamedTypeSymbol type)
    {
        List<string> clauses = new();
        foreach (ITypeParameterSymbol typeParameter in type.TypeParameters)
        {
            List<string> constraints = new();
            if (typeParameter.HasReferenceTypeConstraint)
            {
                constraints.Add("class");
            }
            else if (typeParameter.HasUnmanagedTypeConstraint)
            {
                constraints.Add("unmanaged");
            }
            else if (typeParameter.HasValueTypeConstraint)
            {
                constraints.Add("struct");
            }
            else if (typeParameter.HasNotNullConstraint)
            {
                constraints.Add("notnull");
            }

            constraints.AddRange(typeParameter.ConstraintTypes.Select(x =>
                x.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)));

            if (typeParameter.HasConstructorConstraint && !typeParameter.HasValueTypeConstraint)
            {
                constraints.Add("new()");
            }

            if (constraints.Count > 0)
            {
                clauses.Add($"where {Escape(typeParameter.Name)} : {string.Join(", ", constraints)}");
            }
        }

        return string.Join(" ", clauses);
    }

    private static string GetKindText(INamedTypeSymbol type)
    {
        if (type.IsRecord && type.TypeKind == TypeKind.Struct)
        {
            return "record struct";
        }

        if (type.IsRecord)
        {
            return "record";
        }

        if (type.TypeKind == TypeKind.Interface)
        {
            return "interface";
        }

        return type.TypeKind == TypeKind.Struct ? "struct" : "class";
    }

    private static string GetAccessibilityText(Accessibility accessibility)
    {
        return accessibility switch
        {
            Microsoft.CodeAnalysis.Accessibility.Public => "public",
            Microsoft.CodeAnalysis.Accessibility.Private => "private",
            Microsoft.CodeAnalysis.Accessibility.Internal => "internal",
            Microsoft.CodeAnalysis.Accessibility.Protected => "protected",
            Microsoft.CodeAnalysis.Accessibility.ProtectedOrInternal => "protected internal",
            Microsoft.CodeAnalysis.Accessibility.ProtectedAndInternal => "private protected",
            _ => string.Empty
        };
    }

    private static string GetTypeParameterSuffix(INamedTypeSymbol type)
    {
        if (type.TypeParameters.Length == 0)
        {
            return string.Empty;
        }

        return $"<{string.Join(", ", type.TypeParameters.Select(x => Escape(x.Name)))}>";
    }

    private static string Escape(string identifier)
    {
        return SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;
    }
}

/// <summary>
/// Symbol-free description of the type a constructor is generated for.
/// </summary>
internal record TypeModel(TypeDeclarationModel Declaration,
    string HintName,
    string? Namespace,
    EquatableArray<TypeDeclarationModel> ContainingTypes)
{
    public string Name => Declaration.Name;

    public static TypeModel Create(INamedTypeSymbol type)
    {
        return new TypeModel(TypeDeclarationModel.Create(type),
            GetHintName(type),
            type.ContainingNamespace.IsGlobalNamespace ? null : type.ContainingNamespace.ToDisplayString(),
            GetContainingTypes(type).ToEquatableArray());
    }

    private static IEnumerable<TypeDeclarationModel> GetContainingTypes(INamedTypeSymbol type)
    {
        List<TypeDeclarationModel> containingTypes = new();
        INamedTypeSymbol? containingType = type.ContainingType;
        while (containingType != null)
        {
            containingTypes.Insert(0, TypeDeclarationModel.Create(containingType));
            containingType = containingType.ContainingType;
        }

        return containingTypes;
    }

    private static string GetHintName(INamedTypeSymbol type)
    {
        string namespaceText = type.ContainingNamespace.IsGlobalNamespace
            ? "global"
            : type.ContainingNamespace.ToDisplayString();

        string containingTypePrefix = type.ContainingType == null
            ? string.Empty
            : $"{GetContainingTypePath(type.ContainingType)}+";

        return $"{namespaceText}.{containingTypePrefix}{type.MetadataName}_ConstructorGenerator.g.cs";
    }

    private static string GetContainingTypePath(INamedTypeSymbol containingType)
    {
        if (containingType.ContainingType == null)
        {
            return containingType.MetadataName;
        }

        return $"{GetContainingTypePath(containingType.ContainingType)}+{containingType.MetadataName}";
    }
}
