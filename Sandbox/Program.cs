
using ConstructorGenerator;
namespace Sandbox;

internal abstract partial class TestClass<T1, T2, T3, T4>
{
    [ConstructorDependency]
    private readonly Semaphore _sem;


    
}

public static class Program
{
    public static void Main(string[] args)
    {
        
    }
}
