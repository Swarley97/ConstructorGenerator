using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class MemberSelectionTests
{
    [Test]
    public Task StaticMembersAreIgnored()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public static readonly string StaticField = string.Empty;
                public static string StaticProperty { get; }
                public readonly string Instance;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ConstFieldWithExplicitDependencyIsIgnored()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public partial class Sample
            {
                [ConstructorDependency]
                public const string Constant = "value";

                [ConstructorDependency]
                public readonly string Instance;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task NonAutoPropertiesAreIgnored()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                private readonly string _backing;

                public string ExpressionBodied => _backing;

                public string ExplicitGetter
                {
                    get { return _backing; }
                }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task PropertyWithPrivateSetterIsExcludedFromFullConstructor()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public string PrivateSetter { get; private set; }
                public string ReadOnly { get; }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task InitializedFieldWithExplicitDependencyIsIncluded()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                [ConstructorDependency]
                public readonly string Overridden = "default";

                public readonly string Skipped = "default";
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ExcludeWinsOverExplicitDependency()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public partial class Sample
            {
                [ConstructorDependency]
                [ExcludeConstructorDependency]
                public readonly string Conflicting;

                [ConstructorDependency]
                public readonly string Included;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task EventsAndMethodsAreIgnored()
    {
        const string source = """
            using System;
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class Sample
            {
                public event EventHandler? Changed;
                public readonly string Value;

                public void DoSomething()
                {
                }
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
