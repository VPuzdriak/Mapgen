using System;

namespace Mapgen.Tests.Unit.FieldsMapping.Models.Contracts;

public class PersonDto
{
  public readonly Guid Id;
  public required string FullName { get; init; } = "Unknown";

  public PersonDto(Guid id)
  {
    Id = id;
  }
}
