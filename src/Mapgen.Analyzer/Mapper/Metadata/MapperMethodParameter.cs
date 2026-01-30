using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata
{
  public sealed class MapperMethodParameter
  {
    public IParameterSymbol Symbol { get; }
    public string TypeSyntax { get; }
    public string Name => Symbol.Name;

    public MapperMethodParameter(IParameterSymbol symbol, string typeSyntax)
    {
      Symbol = symbol;
      TypeSyntax = typeSyntax;
    }
  }
}
