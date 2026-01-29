using System;

namespace Mapgen.Tests.Unit.Inheritance.Models;

public sealed class Car : Vehicle, IIdentifiable
{
  public required Guid Id { get; init; }

  public required int NumberOfDoors { get; init; }
}

public abstract class Vehicle
{
  public required string Make { get; init; }
  public required string Model { get; init; }
}
