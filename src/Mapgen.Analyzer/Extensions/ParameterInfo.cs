using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Extensions;

public sealed class ParameterInfo
{
  public IParameterSymbol Symbol { get; }
  public string TypeSyntax { get; }
  public string Name => Symbol.Name;

  public ParameterInfo(IParameterSymbol symbol, string typeSyntax)
  {
    Symbol = symbol;
    TypeSyntax = typeSyntax;
  }
}
