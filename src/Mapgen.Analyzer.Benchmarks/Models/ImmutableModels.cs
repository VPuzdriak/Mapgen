namespace Mapgen.Analyzer.Benchmarks.Models;

/// <summary>
/// Immutable models with constructors - for constructor mapping benchmark
/// </summary>
public class ImmutableEntity
{
  public int Id { get; set; }
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public int Age { get; set; }
  public string Email { get; set; } = string.Empty;
  public DateTime DateOfBirth { get; set; }
  public string Address { get; set; } = string.Empty;
}

public class ImmutableDto
{
  public int Id { get; }
  public string FirstName { get; }
  public string LastName { get; }
  public int Age { get; }
  public string Email { get; }
  public required string Address { get; init; }

  public ImmutableDto(int id, string firstName, string lastName, int age, string email)
  {
    Id = id;
    FirstName = firstName;
    LastName = lastName;
    Age = age;
    Email = email;
  }
}
