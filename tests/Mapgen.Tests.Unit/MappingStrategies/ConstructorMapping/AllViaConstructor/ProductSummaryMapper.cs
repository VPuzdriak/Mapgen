using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AllViaConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AllViaConstructor;

/// <summary>
/// Mapper with all properties set via constructor
/// </summary>
[Mapper]
public partial class AllViaConstructorProductMapper
{
  public partial ProductSummaryDto ToDto(Product source);

  public AllViaConstructorProductMapper()
  {
    UseConstructor(
      source => source.Name,
      source => source.Price
    );
  }
}
