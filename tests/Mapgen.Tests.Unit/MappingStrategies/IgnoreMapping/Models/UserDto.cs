using System;

namespace Mapgen.Tests.Unit.MappingStrategies.IgnoreMapping.Models;

public class UserDto
{
  public required Guid Id { get; init; }
  public required string Username { get; init; }
  public string? Nationality { get; init; }
}
