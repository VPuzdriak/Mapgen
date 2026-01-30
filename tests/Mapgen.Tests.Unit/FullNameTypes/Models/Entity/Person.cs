using System;

namespace Mapgen.Tests.Unit.FullNameTypes.Models.Entity;

public class Person
{
  public required Guid Id { get; init; }
  public required string FirstName { get; init; }
  public required string LastName { get; init; }
}
