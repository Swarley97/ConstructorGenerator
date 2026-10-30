using Microsoft.CodeAnalysis;

namespace ConstructorGenerator;

internal static class Diagnostics
{
    private const string Category = "ConstructorGenerator";

    public static readonly DiagnosticDescriptor TypeMustBePartial = new(
        id: "CG0001",
        title: "Type must be partial",
        messageFormat: "Type '{0}' must be declared partial for a constructor to be generated",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public static readonly DiagnosticDescriptor ConstructorAlreadyExists = new(
        id: "CG0002",
        title: "Constructor already exists",
        messageFormat: "Type '{0}' already declares a constructor with the same signature as the generated one; generation was skipped",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true);
}
