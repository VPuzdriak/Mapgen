using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.CustomMapping.NestedProperty.Models;

namespace Mapgen.Tests.Unit.CustomMapping.NestedProperty;

[Mapper]
public partial class EmployeeMapper
{
  public partial EmployeeDto ToDto(Employee source);

  public EmployeeMapper()
  {
    MapMember(dto => dto.Department, source => source.Department.ToDto());
  }
}
