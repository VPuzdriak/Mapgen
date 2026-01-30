using System;

namespace Mapgen.Tests.Unit.FieldsMapping.Models.Entities;

public class Person
{
  public Guid Id = Guid.NewGuid();
  public string FirstName = "Unknown";
  public string LastName = "Unknown";
}
