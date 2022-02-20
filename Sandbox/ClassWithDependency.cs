using ConstructorGenerator;

namespace Sandbox;

public partial class ClassWithDependency
{
    [ConstructorDependency]
    private string _dependencyOne;

    [ConstructorDependency]
    private int _dependenyTwo;
}