using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata;

/// <summary>
/// Represents information about a constructor to be used for mapping.
/// </summary>
public sealed class ConstructorInfo
{
  public IMethodSymbol Constructor { get; }
  public IReadOnlyList<ConstructorParameterInfo> Parameters { get; }

  public ConstructorInfo(IMethodSymbol constructor)
  {
    Constructor = constructor;
    Parameters = constructor.Parameters
      .Select(p => new ConstructorParameterInfo(p))
      .ToList();
  }
}
