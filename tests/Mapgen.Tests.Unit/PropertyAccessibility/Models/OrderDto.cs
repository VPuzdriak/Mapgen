namespace Mapgen.Tests.Unit.PropertyAccessibility.Models;

/// <summary>
/// DTO for order information - allows setting all fields for API responses
/// </summary>
public class OrderDto
{
  public required string OrderNumber { get; init; }
  public required Guid CustomerId { get; init; }
  public required DateTime OrderDate { get; init; }
  public decimal TotalAmount { get; set; }
  public string Status { get; set; } = "Pending";
  public string? ShippingAddress { get; set; }
  public string? TrackingNumber { get; set; }

  // Formatted display field
  public string OrderSummary => $"Order {OrderNumber} - {Status}";
}
