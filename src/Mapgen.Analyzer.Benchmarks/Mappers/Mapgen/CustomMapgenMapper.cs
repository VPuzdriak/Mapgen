using Mapgen.Analyzer.Benchmarks.Models;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapgen;

[Mapper]
public partial class CustomMapgenMapper
{
  public partial CustomDto ToDto(CustomEntity entity);

  public CustomMapgenMapper()
  {
    MapMember(dto => dto.FullName, entity => $"{entity.FirstName} {entity.LastName}");
    MapMember(dto => dto.AnnualSalary, entity => entity.Salary * 12);
    MapMember(dto => dto.YearsOfService, entity => CalculateYearsOfService(entity.HireDate));
    MapMember(dto => dto.Status, entity => entity.IsActive ? "Active" : "Inactive");
  }

  private static int CalculateYearsOfService(DateTime hireDate)
  {
    return (DateTime.Now - hireDate).Days / 365;
  }
}
