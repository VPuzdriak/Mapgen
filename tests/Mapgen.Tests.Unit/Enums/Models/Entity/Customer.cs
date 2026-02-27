namespace Mapgen.Tests.Unit.Enums.Models.Entity;

public class Customer
{
  public required int Id { get; init; }
  public required CustomerStatus Status { get; init; }
}

public enum CustomerStatus
{
  Regular,
  Vip,
}
