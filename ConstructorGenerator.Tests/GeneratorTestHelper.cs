using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ConstructorGenerator.Attributes;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using VerifyNUnit;
using VerifyTests;

namespace ConstructorGenerator.Tests;

internal static class GeneratorTestHelper
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.CSharp10);

    public static SettingsTask Verify(params string[] sources)
    {
        CSharpCompilation compilation = CreateCompilation(sources);

        GeneratorDriver driver = CreateDriver();

        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out Compilation outputCompilation,
            out ImmutableArray<Diagnostic> diagnostics);
        GeneratorDriverRunResult runResult = driver.GetRunResult();

        GeneratorSnapshot snapshot = new(
            runResult.Results
                .SelectMany(x => x.GeneratedSources)
                .OrderBy(x => x.HintName, StringComparer.Ordinal)
                .Select(x => new GeneratedSourceSnapshot(x.HintName, x.SourceText.ToString()))
                .ToArray(),
            diagnostics
                .OrderBy(x => x.Id, StringComparer.Ordinal)
                .Select(x => x.ToString())
                .ToArray(),
            outputCompilation.GetDiagnostics()
                .Where(x => x.Severity == DiagnosticSeverity.Error)
                .OrderBy(x => x.ToString(), StringComparer.Ordinal)
                .Select(x => x.ToString())
                .ToArray());

        return Verifier.Verify(snapshot);
    }

    public static CSharpCompilation CreateCompilation(params string[] sources)
    {
        return CSharpCompilation.Create(
            assemblyName: "GeneratorTests",
            syntaxTrees: sources.Select(x => CSharpSyntaxTree.ParseText(x, ParseOptions)),
            references: GetMetadataReferences(),
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable,
                allowUnsafe: true));
    }

    public static GeneratorDriver CreateDriver(bool trackIncrementalGeneratorSteps = false)
    {
        var generator = new ConstructorGenerator();
        return CSharpGeneratorDriver.Create(
            new[] { generator.AsSourceGenerator() },
            parseOptions: ParseOptions,
            driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None,
                trackIncrementalGeneratorSteps));
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        string trustedAssemblies = (string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!;

        return trustedAssemblies
            .Split(Path.PathSeparator)
            .Select(x => MetadataReference.CreateFromFile(x))
            .Append(MetadataReference.CreateFromFile(typeof(GenerateFullConstructorAttribute).Assembly.Location));
    }

    private sealed record GeneratorSnapshot(IReadOnlyCollection<GeneratedSourceSnapshot> GeneratedSources,
        IReadOnlyCollection<string> Diagnostics,
        IReadOnlyCollection<string> CompilationErrors);

    private sealed record GeneratedSourceSnapshot(string HintName, string Source);
}
