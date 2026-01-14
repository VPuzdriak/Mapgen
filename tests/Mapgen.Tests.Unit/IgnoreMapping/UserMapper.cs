using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.IgnoreMapping.Models;

namespace Mapgen.Tests.Unit.IgnoreMapping;

[Mapper]
public partial class UserMapper
{
  public partial UserDto ToDto(User source);

  public UserMapper()
  {
    IgnoreMember(dto => dto.Nationality);
  }
}
