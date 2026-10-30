using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ConstructorGenerator.Model;

/// <summary>
/// Symbol-free, equatable representation of a diagnostic so it can flow through the incremental pipeline.
/// </summary>
internal record DiagnosticInfo(DiagnosticDescriptor Descriptor,
    string? FilePath,
    TextSpan TextSpan,
    LinePositionSpan LineSpan,
    EquatableArray<string> MessageArgs)
{
    public static DiagnosticInfo Create(DiagnosticDescriptor descriptor, Location location, params string[] messageArgs)
    {
        return new DiagnosticInfo(descriptor,
            location.SourceTree?.FilePath,
            location.SourceSpan,
            location.GetLineSpan().Span,
            messageArgs.ToEquatableArray());
    }

    public Diagnostic ToDiagnostic()
    {
        Location location = FilePath == null
            ? Location.None
            : Location.Create(FilePath, TextSpan, LineSpan);

        return Diagnostic.Create(Descriptor, location, MessageArgs.ToArray<object>());
    }
}
