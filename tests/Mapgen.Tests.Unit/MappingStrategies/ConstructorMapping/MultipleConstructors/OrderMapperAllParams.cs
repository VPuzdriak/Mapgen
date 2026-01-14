using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.MultipleConstructors.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.MultipleConstructors;

/// <summary>
/// Mapper selecting constructor with 3 parameters
/// </summary>
[Mapper]
public partial class OrderMapperAllParams
{
  public partial OrderDto ToDto(Order source);

  public OrderMapperAllParams()
  {
    UseConstructor(
      source => source.OrderId,
      source => source.Status,
      source => source.Total
    );
  }
}
