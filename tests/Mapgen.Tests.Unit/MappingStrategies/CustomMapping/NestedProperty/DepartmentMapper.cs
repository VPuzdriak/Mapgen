using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.NestedProperty.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.NestedProperty;

[Mapper]
public partial class DepartmentMapper
{
  public partial DepartmentDto ToDto(Department source);
}
