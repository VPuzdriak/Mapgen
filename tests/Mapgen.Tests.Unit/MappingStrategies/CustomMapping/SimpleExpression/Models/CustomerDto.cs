using System;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.SimpleExpression.Models;

public class CustomerDto
{
  public required Guid Id { get; init; }
  public required string FullName { get; init; }
}
