using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Extensions;

public sealed class ParameterInfo
{
  public string Name { get; }
  public ITypeSymbol TypeSymbol { get; }

  public ParameterInfo(string name, ITypeSymbol typeSymbol)
  {
    Name = name;
    TypeSymbol = typeSymbol;
  }
}
