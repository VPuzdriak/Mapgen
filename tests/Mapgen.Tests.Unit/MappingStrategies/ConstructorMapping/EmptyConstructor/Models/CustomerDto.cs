namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.EmptyConstructor.Models;

/// <summary>
/// DTO with both parameterless and parameterized constructors
/// </summary>
public class CustomerDto
{
  public string Name { get; set; }
  public string Email { get; set; }

  // Parameterless constructor
  public CustomerDto()
  {
    Name = string.Empty;
    Email = string.Empty;
  }

  // Parameterized constructor
  public CustomerDto(string name, string email)
  {
    Name = name;
    Email = email;
  }
}
