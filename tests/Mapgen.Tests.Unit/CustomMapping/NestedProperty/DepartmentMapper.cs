using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.CustomMapping.NestedProperty.Models;

namespace Mapgen.Tests.Unit.CustomMapping.NestedProperty;

[Mapper]
public partial class DepartmentMapper
{
  public partial DepartmentDto ToDto(Department source);
}
