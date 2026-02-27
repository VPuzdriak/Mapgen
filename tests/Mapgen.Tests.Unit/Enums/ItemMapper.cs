using Mapgen.Analyzer;
using Mapgen.Tests.Unit.Enums.Models.Contracts;
using Mapgen.Tests.Unit.Enums.Models.Entity;

namespace Mapgen.Tests.Unit.Enums;

[Mapper]
public partial class ItemMapper
{
  public partial ItemDto ToDto(Item item);
}

