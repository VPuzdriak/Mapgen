namespace Mapgen.Tests.Unit.Aliasing.Models.Contract;

public class Person
{
  public required Guid Id { get; init; }
  public required string FullName { get; init; }
  public required Person? Partner { get; init; }
}
