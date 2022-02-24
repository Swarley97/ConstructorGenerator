namespace ConstructorGenerator.Attributes;

public static class WellKnownAttributes
{
    public static AttributeDefinition ConstructorDependencyAttribute { get; }
        = new AttributeDefinition("ConstructorDependencyAttribute");

    public static AttributeDefinition GenerateBaseConstructorCallAttribute { get; }
        = new AttributeDefinition("GenerateBaseConstructorCallAttribute");

    public static AttributeDefinition GeneratedConstructorSettingsAttribute { get; }
        = new AttributeDefinition("GeneratedConstructorSettingsAttribute");
}