using ConstructorGenerator;

namespace Sandbox;

public partial class ClassWithDependency : Base
{
    [ConstructorDependency]
    private string _dependencyOne;

    [ConstructorDependency]
    private int _dependenyTwo;
}

public class Base
{
    public Base(decimal d)
    {
        
    }
}