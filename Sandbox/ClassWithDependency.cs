using ConstructorGenerator.Attributes;

namespace Sandbox;

[GeneratedConstructorSettings(ConstructorAccessibility = ConstructorAccessibility.SameAsClass)]
internal abstract partial class ClassWithDependency : Base
{
    [ConstructorDependency]
    private string _dependencyOne;

    [ConstructorDependency]
    private int DependencyTwo { get; }
}


internal partial struct Triangle
{
    [ConstructorDependency(IsOptional = true)]
    private double A;
}

internal partial record DataClass
{
    [ConstructorDependency]
    public string A;
}

public class Base
{
    public Base(decimal d)
    {
        
    }
}