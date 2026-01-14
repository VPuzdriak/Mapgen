using Mapgen.Analyzer.Benchmarks.Models;

using Mapster;

namespace Mapgen.Analyzer.Benchmarks.Mappers.Mapster;

public static class CustomMapperConfig
{
  public static void Configure()
  {
    // Custom mapping with transformations
    TypeAdapterConfig<CustomEntity, CustomDto>.NewConfig()
        .Map(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}")
        .Map(dest => dest.AnnualSalary, src => src.Salary * 12)
        .Map(dest => dest.YearsOfService, src => (DateTime.Now - src.HireDate).Days / 365)
        .Map(dest => dest.Status, src => src.IsActive ? "Active" : "Inactive");
  }
}
