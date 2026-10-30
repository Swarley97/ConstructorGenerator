using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ConstructorGenerator.Model;

/// <summary>
/// Symbol-free description of a single (possibly containing) type declaration.
/// </summary>
internal record TypeDeclarationModel(string Name,
    string Kind,
    string Accessibility,
    string TypeParameterSuffix)
{
    public static TypeDeclarationModel Create(INamedTypeSymbol type)
    {
        return new TypeDeclarationModel(type.Name,
            GetKindText(type),
            GetAccessibilityText(type.DeclaredAccessibility),
            GetTypeParameterSuffix(type));
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

        return $"<{string.Join(", ", type.TypeParameters.Select(x => x.Name))}>";
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
            : $"{GetContainingTypePath(type.ContainingType)}.";

        return $"{namespaceText}.{containingTypePrefix}{type.MetadataName}_ConstructorGenerator.g.cs";
    }

    private static string GetContainingTypePath(INamedTypeSymbol containingType)
    {
        if (containingType.ContainingType == null)
        {
            return containingType.MetadataName;
        }

        return $"{GetContainingTypePath(containingType.ContainingType)}.{containingType.MetadataName}";
    }
}
