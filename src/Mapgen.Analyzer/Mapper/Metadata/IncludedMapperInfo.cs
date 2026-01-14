using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata
{
  public sealed class IncludedMapperInfo
  {
    public INamedTypeSymbol MapperType { get; }
    public string FieldName { get; }

    public IncludedMapperInfo(INamedTypeSymbol mapperType)
    {
      MapperType = mapperType;
      FieldName = GetMapperFieldName(mapperType);
    }

    private static string GetMapperFieldName(INamedTypeSymbol mapperType)
    {
      // Convert MapperName to _mapperName (e.g., CarMapper -> _carMapper)
      var name = mapperType.Name;
      return $"_{char.ToLowerInvariant(name[0])}{name.Substring(1)}";
    }
  }
}

