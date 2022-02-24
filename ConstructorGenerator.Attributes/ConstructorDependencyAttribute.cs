namespace ConstructorGenerator.Attributes;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ConstructorDependencyAttribute : Attribute
{
    public bool IsOptional { get; set; }
}