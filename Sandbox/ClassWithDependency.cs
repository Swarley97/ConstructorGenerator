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
    private readonly List<decimal> _blub = new();

    public Base(decimal d, List<decimal> blub)
    {
        _blub = blub;
    }
}

[GenerateFullConstructor]
internal partial class Test : Base
{
    private readonly List<int> _numbers = new List<int>();

    private List<string> Names { get; } = new List<string>();

    private readonly int _notInitNumber;

    public string NotInitNameWithSetter { get; set; }
    [ConstructorDependency] public string NotInitNameWithSetterButExplicit { get; set; }
    public string NotInitNameWithInit { get; init; }
    public string NotInitNameReadOnly { get; }

    public string ComputedProperty => string.Empty;
}


internal class VeryBaseClass
{
    private readonly string _baseDependency;

    public VeryBaseClass(string baseDependency)
    {
        _baseDependency = baseDependency;
    }
}

[GenerateFullConstructor]
internal partial class IntermediateClass : VeryBaseClass
{
    private readonly int _ownDependency;
}

[GenerateFullConstructor]
internal partial class FinalClass : IntermediateClass
{
    
}