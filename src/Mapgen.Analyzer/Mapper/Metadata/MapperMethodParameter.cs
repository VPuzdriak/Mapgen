using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata
{
  public sealed class MapperMethodParameter
  {
    public IParameterSymbol Symbol { get; }
    public ITypeSymbol TypeSymbol => Symbol.Type;
    public string Name => Symbol.Name;

    public MapperMethodParameter(IParameterSymbol symbol)
    {
      Symbol = symbol;
    }
  }
}
