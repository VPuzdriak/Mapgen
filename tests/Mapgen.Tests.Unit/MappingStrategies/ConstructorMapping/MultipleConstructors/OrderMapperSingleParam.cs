using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.MultipleConstructors.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.MultipleConstructors;

/// <summary>
/// Mapper selecting constructor with 1 parameter
/// </summary>
[Mapper]
public partial class OrderMapperSingleParam
{
  public partial OrderDto ToDto(Order source);

  public OrderMapperSingleParam()
  {
    UseConstructor(source => source.OrderId);
  }
}
