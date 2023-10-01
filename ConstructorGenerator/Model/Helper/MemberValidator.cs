using System.Collections.Generic;
using System.Linq;
using ConstructorGenerator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ConstructorGenerator.Model.Helper;

public class MemberValidator
{
    public bool ShouldGenerateConstructorParameterForMember(ISymbol memberSymbol, bool generateFullConstructorRequested)
    {
        if (WellKnownAttributes.ExcludeConstructorDependencyAttribute.IsDefined(memberSymbol)) 
            return false; // this member is excluded

        if (memberSymbol.IsImplicitlyDeclared)
            return false; // funny compiler field

        AttributeData? constructorDependencyAttributeData = WellKnownAttributes.ConstructorDependencyAttribute.GetAttributeData(memberSymbol);
        if (!generateFullConstructorRequested && constructorDependencyAttributeData == null)
        {
            // GenerateFullConstructorAttribute does not exists for the class and ConstructorDependencyAttribute does not exists for this member
            return false;
        }
        
        if (constructorDependencyAttributeData == null && IsInitialized(memberSymbol))
        {
            return false;
        }
        
        return memberSymbol switch
        {
            IPropertySymbol propertySymbol => ShouldGenerateConstructorForProperty(propertySymbol,
                constructorDependencyAttributeData),
            IFieldSymbol fieldSymbol => ShouldGenerateConstructorForField(fieldSymbol,
                constructorDependencyAttributeData),
            _ => false
        };
    }

    private bool ShouldGenerateConstructorForField(IFieldSymbol fieldSymbol, AttributeData? explicitConstructorDependencyAttributeData)
    {
        return explicitConstructorDependencyAttributeData != null || fieldSymbol.IsReadOnly;
    }

    private bool ShouldGenerateConstructorForProperty(IPropertySymbol propertySymbol, AttributeData? explicitConstructorDependencyAttributeData)
    {
        if (!IsAutoProperty(propertySymbol))
            return false; // non-auto properties should be ignored because there is a backing field.

        // property should be get or init-only OR explicit ConstructorDependencyAttribute is present
        return explicitConstructorDependencyAttributeData != null || IsReadOnlyOrInitOnlyProperty(propertySymbol);
    }
    
    private static bool IsAutoProperty(IPropertySymbol propertySymbol)
    {
        // Get fields declared in the same type as the property
        IEnumerable<IFieldSymbol> fields = propertySymbol.ContainingType.GetMembers().OfType<IFieldSymbol>();

        // Check if one field is associated to
        return fields.Any(field => SymbolEqualityComparer.Default.Equals(field.AssociatedSymbol, propertySymbol));
    }
    
    private static bool IsReadOnlyOrInitOnlyProperty(IPropertySymbol propertySymbol)
    {
        bool setterIsInitOnly = propertySymbol.SetMethod?.IsInitOnly ?? false;
        return propertySymbol.IsReadOnly || setterIsInitOnly;
    }
    
    private bool IsInitialized(ISymbol param)
    {
        foreach (SyntaxReference syntaxReference in param.DeclaringSyntaxReferences)
        {
            SyntaxNode syntaxNode = syntaxReference.GetSyntax();
            return syntaxNode switch
            {
                VariableDeclaratorSyntax variableDeclaration => variableDeclaration.Initializer != null,
                PropertyDeclarationSyntax propertyDeclaration => propertyDeclaration.Initializer != null,
                _ => false
            };
        }

        return false;
    }
}