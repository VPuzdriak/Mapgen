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
public class SimpleMappingBenchmark
{
  private SimpleEntity _entity = null!;
  private IMapper _autoMapper = null!;
  private Mappers.Mapgen.SimpleMapgenMapper _mapgenMapgenMapper = null!;
  private Mappers.Mapperly.SimpleMapper _mapperlyMapper = null!;

  [GlobalSetup]
  public void Setup()
  {
    // Setup test data
    _entity = new SimpleEntity
    {
      Id = 1,
      Name = "Test Product",
      Description = "This is a test product description",
      Price = 99.99m,
      IsActive = true
    };

    // Setup AutoMapper
    var config = new MapperConfiguration(cfg =>
    {
      cfg.AddProfile<Mappers.AutoMapper.BenchmarkProfile>();
    }, new NullLoggerFactory());
    _autoMapper = config.CreateMapper();

    // Setup Mapgen
    _mapgenMapgenMapper = new Mappers.Mapgen.SimpleMapgenMapper();

    // Setup Mapperly
    _mapperlyMapper = new Mappers.Mapperly.SimpleMapper();

    // Setup Mapster
    Mappers.Mapster.SimpleMapperConfig.Configure();
  }

  [Benchmark]
  public SimpleDto Mapgen_Simple()
  {
    return _mapgenMapgenMapper.ToDto(_entity);
  }

  [Benchmark]
  public SimpleDto AutoMapper_Simple()
  {
    return _autoMapper.Map<SimpleDto>(_entity);
  }

  [Benchmark]
  public SimpleDto Mapperly_Simple()
  {
    return _mapperlyMapper.ToDto(_entity);
  }

  [Benchmark]
  public SimpleDto Mapster_Simple()
  {
    return _entity.Adapt<SimpleDto>();
  }

  [Benchmark(Baseline = true)]
  public SimpleDto Manual_Simple()
  {
    return new SimpleDto
    {
      Id = _entity.Id,
      Name = _entity.Name,
      Description = _entity.Description,
      Price = _entity.Price,
      IsActive = _entity.IsActive
    };
  }
}
