namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor.Models;

/// <summary>
/// DTO with multiple constructors - should show ambiguity error
/// </summary>
public class OrderDto
{
  public int Id { get; }
  public string Status { get; }

  public OrderDto()
  {
    Id = 0;
    Status = "Pending";
  }

  public OrderDto(int id, string status)
  {
    Id = id;
    Status = status;
  }
}
