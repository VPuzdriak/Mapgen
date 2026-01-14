using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata
{
  public sealed class MapperMethodParameter
  {
    public IParameterSymbol Symbol { get; }
    public string Name => Symbol.Name;
    public string Type => Symbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
    public string TypeNamespace => Symbol.Type.ContainingNamespace.ToDisplayString();

    public MapperMethodParameter(IParameterSymbol symbol)
    {
      Symbol = symbol;
    }
  }
}
