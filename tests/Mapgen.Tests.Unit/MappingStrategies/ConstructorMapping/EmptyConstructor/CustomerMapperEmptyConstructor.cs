using Mapgen.Analyzer;
using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.EmptyConstructor.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.EmptyConstructor;

/// <summary>
/// Mapper explicitly using parameterless constructor
/// </summary>
[Mapper]
public partial class CustomerMapperEmptyConstructor
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapperEmptyConstructor()
  {
    UseEmptyConstructor();
  }
}
