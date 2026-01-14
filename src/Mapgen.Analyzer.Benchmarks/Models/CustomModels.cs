namespace Mapgen.Analyzer.Benchmarks.Models;

/// <summary>
/// Models with custom mapping logic - for custom transformation benchmark
/// </summary>
public class CustomEntity
{
  public int Id { get; set; }
  public string FirstName { get; set; } = string.Empty;
  public string LastName { get; set; } = string.Empty;
  public decimal Salary { get; set; }
  public DateTime HireDate { get; set; }
  public bool IsActive { get; set; }
}

public class CustomDto
{
  public int Id { get; set; }
  public string FullName { get; set; } = string.Empty;
  public decimal AnnualSalary { get; set; }
  public int YearsOfService { get; set; }
  public string Status { get; set; } = string.Empty;
}
