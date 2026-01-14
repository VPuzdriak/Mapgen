namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.NestedProperty.Models;

public class EmployeeDto
{
  public required Guid Id { get; init; }
  public required string Name { get; init; }
  public required DepartmentDto Department { get; init; }
}
