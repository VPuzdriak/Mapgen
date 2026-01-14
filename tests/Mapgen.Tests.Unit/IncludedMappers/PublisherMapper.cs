using Mapgen.Analyzer.Abstractions;
using Mapgen.Tests.Unit.IncludedMappers.Models;

namespace Mapgen.Tests.Unit.IncludedMappers;

[Mapper]
public partial class PublisherMapper
{
  public partial PublisherDto ToDto(Publisher source);
}
