namespace Mapgen.Analyzer.Benchmarks.Models;

/// <summary>
/// Simple entity with basic properties - for simple mapping benchmark
/// </summary>
public class SimpleEntity
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public bool IsActive { get; set; }
}

public class SimpleDto
{
  public int Id { get; set; }
  public string Name { get; set; } = string.Empty;
  public string Description { get; set; } = string.Empty;
  public decimal Price { get; set; }
  public bool IsActive { get; set; }
}
