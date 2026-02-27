using Mapgen.Tests.Unit.Enums.Models.Contracts.Enums;

namespace Mapgen.Tests.Unit.Enums.Models.Contracts;

public class ItemDto
{
  public int Id;
  public string Name = "Unknown";
  public ItemCategoryDto Category;
  public ItemAvailabilityDto? Availability;
}

