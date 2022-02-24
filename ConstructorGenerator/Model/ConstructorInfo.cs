using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace ConstructorGenerator.Model;

internal record ConstructorInfo(INamedTypeSymbol Type, 
								Accessibility ConstructorAccessibility,
								IReadOnlyCollection<ParameterInfo> Parameters,
								IReadOnlyCollection<ParameterInfo> BaseParameters)
{
	private IReadOnlyCollection<ParameterInfo>? _allParameters;

	public IReadOnlyCollection<ParameterInfo> AllParameters => _allParameters ??= Parameters.Concat(BaseParameters).ToArray();
}