namespace ConstructorGenerator.Attributes;

[AttributeUsage(AttributeTargets.Class)]
public class GeneratedConstructorSettingsAttribute : Attribute
{
    public ConstructorAccessibility ConstructorAccessibility { get; set; } = ConstructorAccessibility.Default;
}