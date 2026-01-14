using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.CustomMapping.SimpleExpression.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.CustomMapping.SimpleExpression;

[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapper()
  {
    MapMember(dto => dto.Id, source => source.CustomerId);
    MapMember(dto => dto.FullName, source => $"{source.FirstName} {source.LastName}");
  }
}
