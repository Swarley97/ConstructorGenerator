using ConstructorGenerator.Attributes;

namespace ConstructorGenerator.Tests.TestClasses;

[GenerateFullConstructor]
public partial class BaseClassWithFullConstructorAttribute
{
    public string BaseAProperty { get; }

    // ReSharper disable once NotAccessedField.Local
    // ReSharper disable once InconsistentNaming
    private readonly string BaseDuplicateNameField;

    [ConstructorDependency]
    public string BaseBField;
}