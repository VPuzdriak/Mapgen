using System.Linq;

using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CollectionMapping.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CollectionMapping;

[Mapper]
public partial class TeamMapper
{
  public partial TeamDto ToDto(Team source, Lead leader);

  public TeamMapper()
  {
    // MapCollection<MemberDto, Member>(dto => dto.Members, src => src.Members, (member, _, ld) => member.ToDto(ld));
    MapCollection<MemberDto, Member>(dto => dto.Members, src => src.Members.Where<Member>(m => m.Name.StartsWith('A')), (member, _, ld) => member.ToDto(ld));
  }
}
