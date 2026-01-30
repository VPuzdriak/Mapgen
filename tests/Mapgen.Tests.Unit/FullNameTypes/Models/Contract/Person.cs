using System;

namespace Mapgen.Tests.Unit.FullNameTypes.Models.Contract;

public class Person
{
  public required Guid Id { get; init; }
  public required string FullName { get; init; }
  public required Person? Partner { get; init; }
}
