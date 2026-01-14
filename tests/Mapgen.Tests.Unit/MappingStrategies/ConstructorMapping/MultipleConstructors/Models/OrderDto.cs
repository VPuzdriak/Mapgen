namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.MultipleConstructors.Models;

/// <summary>
/// DTO with multiple constructors
/// </summary>
public class OrderDto
{
  public int OrderId { get; }
  public string Status { get; }
  public decimal Total { get; }

  // Constructor 1: Takes only orderId
  public OrderDto(int orderId)
  {
    OrderId = orderId;
    Status = "Pending";
    Total = 0;
  }

  // Constructor 2: Takes orderId and status
  public OrderDto(int orderId, string status)
  {
    OrderId = orderId;
    Status = status;
    Total = 0;
  }

  // Constructor 3: Takes all parameters
  public OrderDto(int orderId, string status, decimal total)
  {
    OrderId = orderId;
    Status = status;
    Total = total;
  }
}
