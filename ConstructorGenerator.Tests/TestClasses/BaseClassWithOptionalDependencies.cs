using ConstructorGenerator.Attributes;

namespace ConstructorGenerator.Tests.TestClasses;

public partial class BaseClassWithOptionalDependencies
{
    [ConstructorDependency(IsOptional = true)]
    public readonly string OptionalFieldBase;
    
    [ConstructorDependency(IsOptional = true)]
    public string OptionalPropertyBase { get; }
}