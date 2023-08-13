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

[GenerateFullConstructor]
internal partial class Test : Base
{
    private List<int> _numbers = new List<int>();

    private List<string> Names { get; } = new List<string>();

    private readonly int _notInitNumber;

    public string NotInitNameWithSetter { get; set; }
    [ConstructorDependency] public string NotInitNameWithSetterButExplicit { get; set; }
    public string NotInitNameWithInit { get; init; }
    public string NotInitNameReadOnly { get; }

    public string ComputedProperty => string.Empty;
}