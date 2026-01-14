using Mapgen.Analyzer.Benchmarks.Models;

using Mapster;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapster;

public static class ImmutableMapperConfig
{
  public static void Configure()
  {
    // Immutable mapping with constructor
    TypeAdapterConfig<ImmutableEntity, ImmutableDto>.NewConfig()
        .ConstructUsing(src => new ImmutableDto(
            src.Id,
            src.FirstName,
            src.LastName,
            src.Age,
            src.Email)
        {
          Address = src.Address
        });
  }
}
