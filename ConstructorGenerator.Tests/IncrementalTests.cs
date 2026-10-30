using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace ConstructorGenerator.Tests;

public class IncrementalTests
{
    private const string Source = """
        using ConstructorGenerator.Attributes;

        namespace Demo;

        [GenerateFullConstructor]
        public partial class Sample
        {
            public readonly string Value;
        }
        """;

    [Test]
    public void UnrelatedSyntaxTreeChangeReusesCachedOutput()
    {
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(Source);
        GeneratorDriver driver = GeneratorTestHelper.CreateDriver(trackIncrementalGeneratorSteps: true);
        driver = driver.RunGenerators(compilation);

        SyntaxTree unrelated = CSharpSyntaxTree.ParseText("namespace Demo { public class Unrelated { } }",
            new CSharpParseOptions(LanguageVersion.CSharp10));
        driver = driver.RunGenerators(compilation.AddSyntaxTrees(unrelated));

        IncrementalGeneratorRunStep[] sourceOutputSteps = driver.GetRunResult().Results
            .Single()
            .TrackedOutputSteps
            .SelectMany(x => x.Value)
            .ToArray();

        Assert.That(sourceOutputSteps.SelectMany(x => x.Outputs).Select(x => x.Reason),
            Is.All.EqualTo(IncrementalStepRunReason.Cached));
    }

    [Test]
    public void ChangedMemberRegeneratesOutput()
    {
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(Source);
        GeneratorDriver driver = GeneratorTestHelper.CreateDriver(trackIncrementalGeneratorSteps: true);
        driver = driver.RunGenerators(compilation);

        CSharpCompilation changed = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.Single(),
            CSharpSyntaxTree.ParseText(Source.Replace("string Value", "int Value"),
                new CSharpParseOptions(LanguageVersion.CSharp10)));
        driver = driver.RunGenerators(changed);

        IncrementalGeneratorRunStep[] sourceOutputSteps = driver.GetRunResult().Results
            .Single()
            .TrackedOutputSteps
            .SelectMany(x => x.Value)
            .ToArray();

        Assert.That(sourceOutputSteps.SelectMany(x => x.Outputs).Select(x => x.Reason),
            Is.All.Not.EqualTo(IncrementalStepRunReason.Cached));
    }

    [Test]
    public void ChangedTypeKeepsOutputOfOtherTypeCached()
    {
        string otherSource = Source.Replace("Sample", "Other");
        CSharpCompilation compilation = GeneratorTestHelper.CreateCompilation(Source, otherSource);
        GeneratorDriver driver = GeneratorTestHelper.CreateDriver(trackIncrementalGeneratorSteps: true);
        driver = driver.RunGenerators(compilation);

        CSharpCompilation changed = compilation.ReplaceSyntaxTree(
            compilation.SyntaxTrees.First(),
            CSharpSyntaxTree.ParseText(Source.Replace("string Value", "int Value"),
                new CSharpParseOptions(LanguageVersion.CSharp10)));
        driver = driver.RunGenerators(changed);

        IncrementalStepRunReason[] reasons = driver.GetRunResult().Results
            .Single()
            .TrackedOutputSteps
            .SelectMany(x => x.Value)
            .SelectMany(x => x.Outputs)
            .Select(x => x.Reason)
            .ToArray();

        Assert.That(reasons.Count(x => x == IncrementalStepRunReason.Cached), Is.EqualTo(1));
    }
}
