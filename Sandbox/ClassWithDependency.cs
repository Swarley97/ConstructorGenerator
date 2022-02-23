using ConstructorGenerator.Attributes;

namespace Sandbox;

public partial class ClassWithDependency : Base
{
    [ConstructorDependency(true)]
    private string _dependencyOne;

    [ConstructorDependency(IsOptional = true)]
    private int _dependenyTwo;
}

public class Base
{
    //public Base(decimal d)
    //{
        
    //}
}