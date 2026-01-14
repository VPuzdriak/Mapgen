using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.IgnoreMapping.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.IgnoreMapping;

[Mapper]
public partial class UserMapper
{
  public partial UserDto ToDto(User source);

  public UserMapper()
  {
    IgnoreMember(dto => dto.Nationality);
  }
}
