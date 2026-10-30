using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ConstructorGenerator.Model;

internal enum ConstructorInitializerKind
{
	None,
	Base,
	This
}

internal record ConstructorInfo(TypeModel Type, 
								Accessibility ConstructorAccessibility,
								EquatableArray<ParameterInfo> Parameters,
								ConstructorInitializerKind InitializerKind,
								EquatableArray<ParameterInfo> InitializerParameters,
								EquatableArray<DiagnosticInfo> Diagnostics)
{
	public IEnumerable<ParameterInfo> AllParameters => Parameters.Concat(InitializerParameters);

	public bool RequiresNullableContext => AllParameters.Any(x => x.RequiresNullableContext);
}