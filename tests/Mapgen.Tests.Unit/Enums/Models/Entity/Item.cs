namespace Mapgen.Tests.Unit.Enums.Models.Entity;

public class Item
{
  public int Id;
  public string Name = "Unknown";
  public ItemCategory Category;
  public ItemAvailability? Availability;
}

public enum ItemCategory
{
  Electronics,
  Clothing,
  Food,
  Books
}

public enum ItemAvailability
{
  InStock,
  OutOfStock,
  PreOrder
}

