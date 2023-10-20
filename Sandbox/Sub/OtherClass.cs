using ConstructorGenerator.Attributes;

namespace Sandbox.Sub;

internal partial struct Triangle
{
    [ConstructorDependency(IsOptional = true)]
    private double A;
}