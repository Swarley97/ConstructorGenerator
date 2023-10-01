using ConstructorGenerator.Attributes;

namespace ConstructorGenerator.Tests.TestClasses;

[GenerateFullConstructor]
public partial class BaseClassWithFullConstructorAttribute
{
    public string BaseAProperty { get; }

    [ConstructorDependency]
    public string BaseBField;
}