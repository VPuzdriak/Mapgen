using System;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.NestedProperty.Models;

public class Employee
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required Department Department { get; init; }
}
