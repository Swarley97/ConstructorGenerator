using ConstructorGenerator.Attributes;

namespace ConstructorGenerator.Tests.TestClasses;

[GenerateBaseConstructorCall]
public partial class TestClassWithBaseButNoDependencies : BaseClassWithFullConstructorAttribute
{
    
}