using Mapgen.Analyzer;
using Mapgen.Tests.Unit.Enums.Models.Contracts;
using Mapgen.Tests.Unit.Enums.Models.Entity;

namespace Mapgen.Tests.Unit.Enums;

[Mapper]
public partial class OrderMapper
{
  public partial OrderDto ToDto(Order order);

  public OrderMapper()
  {
    UseConstructor(
      src => src.Id,
      src => src.CustomerId,
      src => MapToOrderPriorityDto(src.OrderPriority)
    );
  }
}
