using AutoMapper;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;

using Mapgen.Analyzer.Benchmarks.Models;

using Mapster;

using Microsoft.Extensions.Logging.Abstractions;

namespace Mapgen.Analyzer.Benchmarks.Benchmarks;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[MarkdownExporter]
public class CustomMappingBenchmark
{
  private CustomEntity _entity = null!;
  private IMapper _autoMapper = null!;
  private Mappers.Mapgen.CustomMapgenMapper _mapgenMapgenMapper = null!;
  private Mappers.Mapperly.CustomMapper _mapperlyMapper = null!;

  [GlobalSetup]
  public void Setup()
  {
    // Setup test data
    _entity = new CustomEntity
    {
      Id = 1,
      FirstName = "Jane",
      LastName = "Smith",
      Salary = 5000m,
      HireDate = new DateTime(2020, 6, 15),
      IsActive = true
    };

    // Setup AutoMapper
    var config = new MapperConfiguration(cfg =>
    {
      cfg.AddProfile<Mappers.AutoMapper.BenchmarkProfile>();
    }, new NullLoggerFactory());
    _autoMapper = config.CreateMapper();

    // Setup Mapgen
    _mapgenMapgenMapper = new Mappers.Mapgen.CustomMapgenMapper();

    // Setup Mapperly
    _mapperlyMapper = new Mappers.Mapperly.CustomMapper();

    // Setup Mapster
    Mappers.Mapster.CustomMapperConfig.Configure();
  }

  [Benchmark]
  public CustomDto Mapgen_Custom()
  {
    return _mapgenMapgenMapper.ToDto(_entity);
  }

  [Benchmark]
  public CustomDto AutoMapper_Custom()
  {
    return _autoMapper.Map<CustomDto>(_entity);
  }

  [Benchmark]
  public CustomDto Mapperly_Custom()
  {
    return _mapperlyMapper.ToDto(_entity);
  }

  [Benchmark]
  public CustomDto Mapster_Custom()
  {
    return _entity.Adapt<CustomDto>();
  }

  [Benchmark(Baseline = true)]
  public CustomDto Manual_Custom()
  {
    return new CustomDto
    {
      Id = _entity.Id,
      FullName = $"{_entity.FirstName} {_entity.LastName}",
      AnnualSalary = _entity.Salary * 12,
      YearsOfService = (DateTime.Now - _entity.HireDate).Days / 365,
      Status = _entity.IsActive ? "Active" : "Inactive"
    };
  }
}
