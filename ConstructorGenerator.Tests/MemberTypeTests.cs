using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class MemberTypeTests
{
    [Test]
    public Task ArrayTypedMembersAreIncluded()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string[] Names;
                public int[][] Matrix { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task GenericBaseWithTypeParameterConstructorParameter()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class BaseType<T>
            {
                public BaseType(T value)
                {
                }
            }

            [GenerateBaseConstructorCall]
            public partial class ChildType : BaseType<int>
            {
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task GenericMemberTypesAreFullyQualified()
    {
        const string source = """
            using System.Collections.Generic;
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly List<string> Items;
                public Dictionary<string, int> Lookup { get; }
                public (int Id, string Name) Tuple { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task NullableReferenceAndValueTypesArePreserved()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly string? OptionalText;
                public int? OptionalNumber { get; }
                public string Required { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task NestedTypeAsMemberTypeIsFullyQualified()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class Outer
            {
                public class Inner
                {
                }
            }

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly Outer.Inner Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task MemberTypeFromAnotherAssemblyIsFullyQualified()
    {
        const string source = """
            using System;
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public readonly Uri Address;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
