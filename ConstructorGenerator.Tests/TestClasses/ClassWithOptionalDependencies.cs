using ConstructorGenerator.Attributes;

namespace ConstructorGenerator.Tests.TestClasses;

[GenerateFullConstructor]
public partial class ClassWithOptionalDependencies : BaseClassWithOptionalDependencies
{
    [ConstructorDependency(IsOptional = true)]
    public readonly string OptionalField;
    
    [ConstructorDependency(IsOptional = true)]
    public string OptionalProperty { get; }
}