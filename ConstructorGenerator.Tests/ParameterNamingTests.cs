using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class ParameterNamingTests
{
    [Test]
    public Task UnderscorePrefixedFieldsGetCamelCaseParameters()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                private readonly string _value;
                private readonly int _count;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task NameCollisionAfterNormalizationIsResolved()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                private readonly string _value;
                public int Value { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task KeywordMemberNamesAreEscaped()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string Class;
                public string Event { get; }
                public object Object { get; init; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ShortAndUpperCaseNamesAreHandled()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly int X;
                public readonly string ID;
                public readonly string _;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task LowerCaseMemberNameIsAssignedThroughThis()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
