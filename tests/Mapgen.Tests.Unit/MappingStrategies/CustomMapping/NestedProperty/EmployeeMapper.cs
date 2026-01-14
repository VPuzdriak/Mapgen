using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.NestedProperty.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.NestedProperty;

[Mapper]
public partial class EmployeeMapper
{
  public partial EmployeeDto ToDto(Employee source);

  public EmployeeMapper()
  {
    MapMember(dto => dto.Department, source => source.Department.ToDto());
  }
}
