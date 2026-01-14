using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor;

/// <summary>
/// Mapper that should automatically use constructor without explicit UseConstructor() call
/// </summary>
[Mapper]
public partial class AutoProductMapper
{
  public partial ProductDto ToDto(Product source);

  // No constructor configuration - should auto-detect and use the parameterized constructor
}
