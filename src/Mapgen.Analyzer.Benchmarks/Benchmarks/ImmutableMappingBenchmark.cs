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
public class ImmutableMappingBenchmark
{
  private ImmutableEntity _entity = null!;
  private IMapper _autoMapper = null!;
  private Mappers.Mapgen.ImmutableMapgenMapper _mapgenMapgenMapper = null!;
  private Mappers.Mapperly.ImmutableMapper _mapperlyMapper = null!;

  [GlobalSetup]
  public void Setup()
  {
    // Setup test data
    _entity = new ImmutableEntity
    {
      Id = 1,
      FirstName = "John",
      LastName = "Doe",
      Age = 30,
      Email = "john.doe@example.com",
      DateOfBirth = new DateTime(1994, 1, 15),
      Address = "123 Main St, New York"
    };

    // Setup AutoMapper
    var config = new MapperConfiguration(cfg =>
    {
      cfg.AddProfile<Mappers.AutoMapper.BenchmarkProfile>();
    }, new NullLoggerFactory());
    _autoMapper = config.CreateMapper();

    // Setup Mapgen
    _mapgenMapgenMapper = new Mappers.Mapgen.ImmutableMapgenMapper();

    // Setup Mapperly
    _mapperlyMapper = new Mappers.Mapperly.ImmutableMapper();

    // Setup Mapster
    Mappers.Mapster.ImmutableMapperConfig.Configure();
  }

  [Benchmark]
  public ImmutableDto Mapgen_Immutable()
  {
    return _mapgenMapgenMapper.ToDto(_entity);
  }

  [Benchmark]
  public ImmutableDto AutoMapper_Immutable()
  {
    return _autoMapper.Map<ImmutableDto>(_entity);
  }

  [Benchmark]
  public ImmutableDto Mapperly_Immutable()
  {
    return _mapperlyMapper.ToDto(_entity);
  }

  [Benchmark]
  public ImmutableDto Mapster_Immutable()
  {
    return _entity.Adapt<ImmutableDto>();
  }

  [Benchmark(Baseline = true)]
  public ImmutableDto Manual_Immutable()
  {
    return new ImmutableDto(
        _entity.Id,
        _entity.FirstName,
        _entity.LastName,
        _entity.Age,
        _entity.Email)
    {
      Address = _entity.Address
    };
  }
}
