using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class TypeShapeTests
{
    [Test]
    public Task GenericRecordClassIncludesTypeParameters()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial record Sample<T>
            {
                public readonly T Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task GlobalNamespaceGeneration()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task NestedTypeGeneration()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public partial class Outer
            {
                [GenerateFullConstructor]
                public partial class Inner
                {
                    public readonly string Value;
                }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task RecordStructGeneration()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial record struct Sample
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task FileNameCollisionForArity()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Foo
            {
                public readonly string Value;
            }

            [GenerateFullConstructor]
            public partial class Foo<T>
            {
                public readonly T Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task DuplicateParameterNamesAcrossInheritance()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class BaseType
            {
                public readonly string Duplicate;
            }

            [GenerateFullConstructor]
            public partial class ChildType : BaseType
            {
                public readonly string Duplicate;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task PlainStructGeneration()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial struct Sample
            {
                public readonly int Value;
                public string Name { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task RecordWithPrimaryConstructorChainsToThis()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial record Sample(string Positional)
            {
                public readonly int Extra;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task MultipleTypeParametersWithConstraints()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample<TKey, TValue>
                where TKey : class
                where TValue : struct
            {
                public readonly TKey Key;
                public TValue Value { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task PartialTypeSplitAcrossSyntaxTrees()
    {
        const string first = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string First;
            }
            """;

        const string second = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public partial class Sample
            {
                [ConstructorDependency]
                public string Second { get; set; }
            }
            """;

        return GeneratorTestHelper.Verify(first, second);
    }

    [Test]
    public Task UserDefinedOnConstructingIsInvoked()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string Value;
                public int Length { get; private set; }

                partial void OnConstructing()
                {
                    Length = Value.Length;
                }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task MultipleTypesInDifferentNamespacesGetUniqueHintNames()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo.First
            {
                [GenerateFullConstructor]
                public partial class Sample
                {
                    public readonly string Value;
                }
            }

            namespace Demo.Second
            {
                [GenerateFullConstructor]
                public partial class Sample
                {
                    public readonly int Value;
                }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task EmptyTypeGeneratesParameterlessConstructor()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public string Mutable { get; set; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
