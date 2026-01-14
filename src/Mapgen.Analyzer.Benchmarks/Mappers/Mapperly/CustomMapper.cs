using Mapgen.Analyzer.Benchmarks.Models;

using Riok.Mapperly.Abstractions;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapperly;

[Riok.Mapperly.Abstractions.Mapper]
public partial class CustomMapper
{
  // Using Mapperly's configuration API with explicit method mappings
  [MapProperty(source: nameof(CustomEntity), target: nameof(CustomDto.FullName), Use = nameof(MapFullName))]
  [MapProperty(source: nameof(CustomEntity), target: nameof(CustomDto.AnnualSalary), Use = nameof(MapAnnualSalary))]
  [MapProperty(source: nameof(CustomEntity), target: nameof(CustomDto.YearsOfService), Use = nameof(MapYearsOfService))]
  [MapProperty(source: nameof(CustomEntity), target: nameof(CustomDto.Status), Use = nameof(MapStatus))]
  public partial CustomDto ToDto(CustomEntity entity);

  // Custom mapping for FullName - combines FirstName and LastName
  private string MapFullName(CustomEntity entity)
  {
    return $"{entity.FirstName} {entity.LastName}";
  }

  // Custom mapping for AnnualSalary - multiply monthly salary by 12
  private decimal MapAnnualSalary(CustomEntity entity)
  {
    return entity.Salary * 12;
  }

  // Custom mapping for YearsOfService - calculate from hire date
  private int MapYearsOfService(CustomEntity entity)
  {
    return (DateTime.Now - entity.HireDate).Days / 365;
  }

  // Custom mapping for Status - convert boolean to string
  private string MapStatus(CustomEntity entity)
  {
    return entity.IsActive ? "Active" : "Inactive";
  }
}
