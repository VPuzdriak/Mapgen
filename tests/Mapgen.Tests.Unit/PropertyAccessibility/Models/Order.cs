using System;

namespace Mapgen.Tests.Unit.PropertyAccessibility.Models;

/// <summary>
/// Order entity with immutable order number and status management
/// </summary>
public class Order
{
  // Order number is set once and never changed
  public required string OrderNumber { get; init; }
  public required Guid CustomerId { get; init; }
  public required DateTime OrderDate { get; init; }

  // Total is calculated from order items (read-only from outside)
  public decimal TotalAmount { get; private set; }

  // Status can only be changed through business logic (private setter)
  public string Status { get; private set; } = "Pending";

  // Shipping info is mutable
  public string? ShippingAddress { get; set; }
  public string? TrackingNumber { get; set; }

  public void UpdateTotal(decimal amount) => TotalAmount = amount;
  public void UpdateStatus(string newStatus) => Status = newStatus;
}
