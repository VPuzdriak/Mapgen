namespace Mapgen.Tests.Unit.MappingStrategies.IgnoreMapping.Models;

public class User
{
  public required Guid Id { get; init; }
  public required string Username { get; init; }
  public required string Password { get; init; }
}
