using System.Threading.Tasks;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class InheritanceTests
{
    [Test]
    public Task GenerateBaseConstructorCallUsesBaseParameters()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class BaseType
            {
                public BaseType(string baseA, int baseB)
                {
                }
            }

            [GenerateBaseConstructorCall]
            public partial class ChildType : BaseType
            {
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task FullConstructorIncludesOwnAndBaseParameters()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class BaseType
            {
                public readonly string BaseValue;
            }

            [GenerateFullConstructor]
            public partial class ChildType : BaseType
            {
                public readonly int ChildValue;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ThreeLevelHierarchyChainsAllParameters()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class GrandParent
            {
                public readonly string GrandValue;
            }

            [GenerateFullConstructor]
            public partial class Parent : GrandParent
            {
                public readonly int ParentValue;
            }

            [GenerateFullConstructor]
            public partial class Child : Parent
            {
                public readonly bool ChildValue;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task BaseWithMultipleConstructorsUsesTheLargest()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class BaseType
            {
                public BaseType()
                {
                }

                public BaseType(string a)
                {
                }

                public BaseType(string a, int b)
                {
                }
            }

            [GenerateBaseConstructorCall]
            public partial class ChildType : BaseType
            {
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task BaseWithOnlyParameterlessConstructorGeneratesNoBaseCall()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class BaseType
            {
            }

            [GenerateFullConstructor]
            public partial class ChildType : BaseType
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task BaseOptionalParametersStayOptionalAfterChildParameters()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class BaseType
            {
                public BaseType(string required, int optional = 5)
                {
                }
            }

            [GenerateFullConstructor]
            public partial class ChildType : BaseType
            {
                public readonly bool Flag;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task BaseRefAndParamsParametersAreForwarded()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class BaseType
            {
                public BaseType(in int input, ref string reference, params object[] rest)
                {
                }
            }

            [GenerateBaseConstructorCall]
            public partial class ChildType : BaseType
            {
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task ChildWithoutDependenciesDeclaredBeforeBaseStillChains()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            [GenerateFullConstructor]
            public partial class ChildType : BaseType
            {
            }

            [GenerateFullConstructor]
            public partial class BaseType
            {
                public readonly string Value;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task BaseConstructorCallWithOwnExplicitDependencies()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class BaseType
            {
                public BaseType(string baseValue)
                {
                }
            }

            [GenerateBaseConstructorCall]
            public partial class ChildType : BaseType
            {
                [ConstructorDependency]
                public readonly int Own;

                public readonly bool Ignored;
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }

    [Test]
    public Task BaseWithInaccessiblePrivateConstructorIsNotChained()
    {
        const string source = """
            using ConstructorGenerator.Attributes;

            namespace Demo;

            public class BaseType
            {
                private BaseType(string hidden)
                {
                }

                protected BaseType(int visible)
                {
                }
            }

            [GenerateBaseConstructorCall]
            public partial class ChildType : BaseType
            {
            }
            """;

        return GeneratorTestHelper.Verify(source);
    }
}
