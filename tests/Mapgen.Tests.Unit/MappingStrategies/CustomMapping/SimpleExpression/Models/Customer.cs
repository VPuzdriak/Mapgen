using System;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.SimpleExpression.Models;

public class Customer
{
  public required Guid CustomerId { get; init; }
  public required string FirstName { get; init; }
  public required string LastName { get; init; }
}
