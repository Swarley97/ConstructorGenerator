using Microsoft.CodeAnalysis;

namespace ConstructorGenerator.Model;

internal record ParameterInfo(INamedTypeSymbol Type,
    string? Name,
    string? AssignmentTargetMemberName,
    bool IsOptional);