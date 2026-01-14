using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.BasicConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.BasicConstructor;

/// <summary>
/// Mapper using constructor with 3 parameters and object initializer for remaining properties
/// </summary>
[Mapper]
public partial class BasicConstructorProductMapper
{
  public partial ProductDto ToDto(Product source);

  public BasicConstructorProductMapper()
  {
    UseConstructor(
      source => source.Name,
      source => source.Description,
      source => source.Price
    );
    // Stock and Category should be mapped via object initializer
  }
}
