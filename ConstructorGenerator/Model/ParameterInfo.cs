using Microsoft.CodeAnalysis;

namespace ConstructorGenerator.Model;

/// <summary>
/// Symbol-free description of a generated constructor parameter.
/// </summary>
/// <param name="Type">Fully qualified type text including nullable annotations.</param>
/// <param name="Name">Explicit parameter name (used for base/this constructor parameters).</param>
/// <param name="AssignmentTargetMemberName">Member that gets assigned from this parameter, if any.</param>
/// <param name="IsOptional">Whether the parameter gets a <c>default</c> value.</param>
/// <param name="RefKind">Ref kind forwarded from a base constructor parameter.</param>
/// <param name="IsParams">Whether the parameter is a <c>params</c> array.</param>
/// <param name="RequiresNullableContext">Whether the type text contains a nullable reference type annotation.</param>
internal record ParameterInfo(string Type,
    string? Name,
    string? AssignmentTargetMemberName,
    bool IsOptional,
    RefKind RefKind = RefKind.None,
    bool IsParams = false,
    bool RequiresNullableContext = false);