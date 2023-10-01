using ConstructorGenerator.Attributes;
// ReSharper disable All //its a test class

namespace ConstructorGenerator.Tests.TestClasses;

[GenerateFullConstructor]
public partial class TestClassWithBaseClassAndFullConstructorAttribute : BaseClassWithFullConstructorAttribute
{
    public readonly string FieldWithInitializer = null!;
    [ConstructorDependency] public readonly string FieldWithInitializerButAttribute = null!;
    
    public string PropertyWithInitializer { get; } = null!;
    [ConstructorDependency] public string PropertyWithInitializerButAttribute { get; } = null!;

    public string PropertyWithInit { get; init; }

    public string PropertyWithSetter { get; set; }
    [ConstructorDependency]
    public string PropertyWithSetterButAttribute { get; set; }
    
    
    public string ComputedProperty => null!;

    public readonly string ReadonlyDependency;
    
    public string PropertyReadOnly { get; }
    
    [ExcludeConstructorDependency]
    public string? PropertyReadOnlyButIgnored { get; }
}