using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConstructorGenerator.Attributes;

public sealed class AttributeDefinition : IEquatable<AttributeDefinition>
{
    private readonly string _attributeFullName;
    private readonly string _attributeName;

    public AttributeDefinition(string attributeFullName)
    {
        _attributeFullName = attributeFullName;

        const string attributeWord = "Attribute";
        if (_attributeFullName.EndsWith(attributeWord))
        {
            _attributeName = _attributeFullName.Remove(_attributeFullName.Length - attributeWord.Length,
                attributeWord.Length);
        }
        else
        {
            _attributeName = _attributeFullName;
        }
    }

    public bool IsDefined(string? attributeName) =>
        string.Equals(_attributeFullName, attributeName, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(_attributeName, attributeName, StringComparison.OrdinalIgnoreCase);

    public bool IsDefined(AttributeListSyntax attributeList)
    {
        return attributeList.Attributes.Any(x => IsDefined(x.Name.ToString()));
    }

    public bool IsDefined(SyntaxList<AttributeListSyntax> classDeclarationAttributeLists)
    {
        return classDeclarationAttributeLists.Any(IsDefined);
    }

    public bool IsDefined(INamedTypeSymbol namedTypeSymbol)
    {
        return namedTypeSymbol.GetAttributes().Any(x => IsDefined(x.AttributeClass?.Name));
    }

    public bool IsDefined(ISymbol namedTypeSymbol)
    {
        return namedTypeSymbol.GetAttributes().Any(x => IsDefined(x.AttributeClass?.Name));
    }

    public AttributeData? GetAttributeData(ISymbol namedTypeSymbol)
    {
        return namedTypeSymbol.GetAttributes().FirstOrDefault(x => IsDefined(x.AttributeClass?.Name));
    }

    public bool Equals(AttributeDefinition? other)
    {
        if (ReferenceEquals(null, other)) return false;
        if (ReferenceEquals(this, other)) return true;

        return string.Equals(_attributeFullName, other._attributeFullName, StringComparison.OrdinalIgnoreCase) &&
               string.Equals(_attributeName, other._attributeName, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj)
    {
        return ReferenceEquals(this, obj) || obj is AttributeDefinition other && Equals(other);
    }

    public override int GetHashCode()
    {
        unchecked
        {
            return (_attributeFullName.GetHashCode() * 397) ^ _attributeName.GetHashCode();
        }
    }
}