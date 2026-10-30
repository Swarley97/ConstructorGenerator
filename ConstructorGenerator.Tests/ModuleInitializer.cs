using System.Runtime.CompilerServices;
using VerifyNUnit;

namespace ConstructorGenerator.Tests;

public static class ModuleInitializer
{
    [ModuleInitializer]
    public static void Init()
    {
        Verifier.UseProjectRelativeDirectory("Snapshots");
    }
}
