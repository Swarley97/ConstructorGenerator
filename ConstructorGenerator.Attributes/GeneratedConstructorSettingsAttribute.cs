namespace ConstructorGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public class GeneratedConstructorSettingsAttribute : Attribute
{
    public ConstructorAccessibility ConstructorAccessibility { get; set; } = ConstructorAccessibility.Default;
}