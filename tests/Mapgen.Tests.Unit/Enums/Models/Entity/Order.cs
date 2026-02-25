using System.Collections.Generic;

namespace Mapgen.Tests.Unit.Enums.Models.Entity;

public sealed class Order
{
  public required int Id { get; init; }
  public required int CustomerId { get; init; }
  public required OrderPriority OrderPriority { get; init; }
  public required OrderStatus? CurrentStatus { get; init; }
  public required List<OrderStatus> StatusHistory { get; init; } = [];
}

public enum OrderPriority
{
  Low,
  Medium,
  High
}

public enum OrderStatus
{
  Shipped,
  Pending,
  Delivered,
  Cancelled
}
