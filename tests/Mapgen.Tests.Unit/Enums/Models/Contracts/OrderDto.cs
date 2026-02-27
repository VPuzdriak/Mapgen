using System.Collections.Immutable;

using Mapgen.Tests.Unit.Enums.Models.Contracts.Enums;

namespace Mapgen.Tests.Unit.Enums.Models.Contracts;

public sealed class OrderDto
{
  public int Id { get; }
  public int CustomerId { get; }
  public bool IsVipOrder { get; }
  public required OrderStatusDto? CurrentStatus { get; init; }
  public required IImmutableList<OrderStatusDto> StatusHistory { get; set; }

  public OrderDto(int id, int customerId, OrderPriorityDto orderPriority, CustomerStatus customerStatus)
  {
    Id = id;
    CustomerId = customerId;
    IsVipOrder = customerStatus == CustomerStatus.Vip || orderPriority == OrderPriorityDto.High;
    StatusHistory = [];
  }

  public OrderDto(OrderPriorityDto orderPriority, int id, int customerId)
  {
    Id = id;
    CustomerId = customerId;
    IsVipOrder = orderPriority == OrderPriorityDto.High;
    StatusHistory = [];
  }
}
