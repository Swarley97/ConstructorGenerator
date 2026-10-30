using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class FullConstructorTests
{
    [Test]
    public Task GeneratesConstructorForReadonlyMembersAndInitProperties()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string ReadonlyField;
                public string ReadonlyProperty { get; }
                public string InitProperty { get; init; }
                public string IgnoredWithInitializer { get; } = string.Empty;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task OptionalParametersAreOrderedLastAndUseDefaultLiteral()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string RequiredField;

                [ConstructorDependency(IsOptional = true)]
                public readonly string OptionalField;

                [ConstructorDependency(IsOptional = true)]
                public string OptionalProperty { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
