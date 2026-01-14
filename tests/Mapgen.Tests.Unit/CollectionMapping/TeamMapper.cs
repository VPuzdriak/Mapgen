using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.CollectionMapping.Models;


namespace Mapgen.Tests.Unit.CollectionMapping;

[Mapper]
public partial class TeamMapper
{
  public partial TeamDto ToDto(Team source, Lead leader);

  public TeamMapper()
  {
    // MapCollection<MemberDto, Member>(dto => dto.Members, src => src.Members, (member, _, ld) => member.ToDto(ld));
    MapCollection<MemberDto, Member>(dto => dto.Members, src => src.Members.Where(m => m.Name.StartsWith("A")), (member, _, ld) => member.ToDto(ld));
  }
}
