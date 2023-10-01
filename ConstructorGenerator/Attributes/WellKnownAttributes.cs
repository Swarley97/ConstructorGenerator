namespace ConstructorGenerator.Attributes;

public static class WellKnownAttributes
{
    public static AttributeDefinition ConstructorDependencyAttribute { get; } = new("ConstructorDependencyAttribute");

    public static AttributeDefinition GenerateBaseConstructorCallAttribute { get; } = new("GenerateBaseConstructorCallAttribute");

    public static AttributeDefinition GeneratedConstructorSettingsAttribute { get; } = new("GeneratedConstructorSettingsAttribute");

    public static AttributeDefinition GenerateFullConstructorAttribute { get; } = new("GenerateFullConstructorAttribute");

    public static AttributeDefinition ExcludeConstructorDependencyAttribute { get; } = new("ExcludeConstructorDependencyAttribute");
}