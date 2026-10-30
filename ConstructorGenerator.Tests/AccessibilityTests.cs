using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class AccessibilityTests
{
    [TestCase("Protected")]
    [TestCase("Internal")]
    [TestCase("Public")]
    public Task ConstructorAccessibilityCanBeSetExplicitly(string accessibility)
    {
        string source = $$"""
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            [GeneratedConstructorSettings(ConstructorAccessibility = ConstructorAccessibility.{{accessibility}})]
            public partial class Sample
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source).UseParameters(accessibility);
    }

    [Test]
    public Task AbstractClassDefaultsToProtectedConstructor()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public abstract partial class Sample
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task AbstractClassExplicitAccessibilityIsRespected()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            [GeneratedConstructorSettings(ConstructorAccessibility = ConstructorAccessibility.Public)]
            public abstract partial class Sample
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task SameAsClassOnNestedPrivateType()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public partial class Outer
            {
                [GenerateFullConstructor]
                [GeneratedConstructorSettings(ConstructorAccessibility = ConstructorAccessibility.SameAsClass)]
                private partial class Inner
                {
                    public readonly string Value;
                }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ConstructorAccessibilityCanBePrivate()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            [GeneratedConstructorSettings(ConstructorAccessibility = ConstructorAccessibility.Private)]
            public partial class Sample
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ConstructorAccessibilityCanMatchClassVisibility()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            [GeneratedConstructorSettings(ConstructorAccessibility = ConstructorAccessibility.SameAsClass)]
            internal partial class Sample
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
