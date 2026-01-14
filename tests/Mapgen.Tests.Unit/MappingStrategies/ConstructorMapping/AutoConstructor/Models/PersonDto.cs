namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor.Models;

/// <summary>
/// DTO with long Age (int can be implicitly converted to long)
/// </summary>
public class PersonDto
{
  public string Name { get; }
  public long Age { get; }

  public PersonDto(string name, long age)
  {
    Name = name;
    Age = age;
  }
}
