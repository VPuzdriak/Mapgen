using System;

namespace Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ImplicitConversion.Models;

public class Contact
{
  public required Guid Id { get; init; }
  public required string Email { get; init; }
  public string? Phone { get; init; }
}
