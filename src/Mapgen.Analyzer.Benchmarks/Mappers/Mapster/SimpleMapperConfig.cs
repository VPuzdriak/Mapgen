using Mapgen.Analyzer.Benchmarks.Models;

using Mapster;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapster;

public static class SimpleMapperConfig
{
  public static void Configure()
  {
    // Simple mapping - Mapster auto-maps by convention
    TypeAdapterConfig<SimpleEntity, SimpleDto>.NewConfig();
  }
}
