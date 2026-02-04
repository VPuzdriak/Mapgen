using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.AutoConstructor;

[Mapper]
public partial class AutoShopMapper
{
  public partial ShopDto ToDto(Shop shop);

  public AutoShopMapper()
  {
    IncludeMappers([new AutoProductMapper()]);
  }
}
