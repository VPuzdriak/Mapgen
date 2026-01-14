using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata;

/// <summary>
/// Represents information about a constructor parameter.
/// </summary>
public sealed class ConstructorParameterInfo
{
  public IParameterSymbol Symbol { get; }
  public string Name { get; }
  public ITypeSymbol Type { get; }
  public bool HasDefaultValue { get; }
  public int Position { get; }

  public ConstructorParameterInfo(IParameterSymbol symbol)
  {
    Symbol = symbol;
    Name = symbol.Name;
    Type = symbol.Type;
    HasDefaultValue = symbol.HasExplicitDefaultValue;
    Position = symbol.Ordinal;
  }
}
