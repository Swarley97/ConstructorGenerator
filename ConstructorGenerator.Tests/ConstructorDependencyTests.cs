using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class ConstructorDependencyTests
{
    [Test]
    public Task ConstructorDependencyIncludesSettableMembers()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public partial class Sample
            {
                [ConstructorDependency]
                public string MutableProperty { get; set; }

                [ConstructorDependency]
                public string MutableField;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ExcludeConstructorDependencySkipsMembers()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string Included;

                [ExcludeConstructorDependency]
                public string Excluded { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
