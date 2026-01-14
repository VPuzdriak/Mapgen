using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ImplicitConversion.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.DirectMapping.ImplicitConversion;

[Mapper]
public partial class ProductMapper
{
  public partial ProductDto ToDto(Product source);
}
