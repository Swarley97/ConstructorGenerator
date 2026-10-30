using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class DiagnosticTests
{
    [Test]
    public Task NonPartialTypeReportsErrorAndSkipsGeneration()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public class Sample
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ExistingConstructorWithSameSignatureReportsWarningAndSkipsGeneration()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string Value;

                public Sample(string value)
                {
                    Value = value;
                }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ExistingConstructorWithDifferentSignatureDoesNotBlockGeneration()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string Value;

                public Sample()
                {
                    Value = string.Empty;
                }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
