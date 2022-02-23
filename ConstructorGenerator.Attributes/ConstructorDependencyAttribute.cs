namespace ConstructorGenerator.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ConstructorDependencyAttribute : Attribute
{
    public ConstructorDependencyAttribute(bool isOptional = false)
    {
        IsOptional = isOptional;
    }
     
    public bool IsOptional { get; set; }
    
    public ConstructorAccessibility Accessibility { get; }
}