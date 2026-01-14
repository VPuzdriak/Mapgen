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
public class ComplexMappingBenchmark
{
  private ComplexEntity _entity = null!;
  private IMapper _autoMapper = null!;
  private Mappers.Mapgen.ComplexMapgenMapper _mapgenMapgenMapper = null!;
  private Mappers.Mapperly.ComplexMapper _mapperlyMapper = null!;

  [GlobalSetup]
  public void Setup()
  {
    // Setup test data with nested objects
    _entity = new ComplexEntity
    {
      Id = 1,
      Name = "Complex Order",
      Address = new Address
      {
        Street = "123 Main St",
        City = "New York",
        ZipCode = "10001",
        Country = "USA"
      },
      Contact = new Contact
      {
        Email = "test@example.com",
        Phone = "+1-555-1234"
      },
      Items = new List<OrderItem>
            {
                new() { ProductId = 1, ProductName = "Product A", Quantity = 2, UnitPrice = 10.99m },
                new() { ProductId = 2, ProductName = "Product B", Quantity = 1, UnitPrice = 25.50m },
                new() { ProductId = 3, ProductName = "Product C", Quantity = 3, UnitPrice = 5.99m }
            },
      CreatedAt = DateTime.Now,
      TotalAmount = 53.95m
    };

    // Setup AutoMapper
    var config = new MapperConfiguration(cfg =>
    {
      cfg.AddProfile<Mappers.AutoMapper.BenchmarkProfile>();
    }, new NullLoggerFactory());
    _autoMapper = config.CreateMapper();

    // Setup Mapgen
    _mapgenMapgenMapper = new Mappers.Mapgen.ComplexMapgenMapper();

    // Setup Mapperly
    _mapperlyMapper = new Mappers.Mapperly.ComplexMapper();

    // Setup Mapster
    Mappers.Mapster.ComplexMapperConfig.Configure();
  }

  [Benchmark]
  public ComplexDto Mapgen_Complex()
  {
    return _mapgenMapgenMapper.ToDto(_entity);
  }

  [Benchmark]
  public ComplexDto AutoMapper_Complex()
  {
    return _autoMapper.Map<ComplexDto>(_entity);
  }

  [Benchmark]
  public ComplexDto Mapperly_Complex()
  {
    return _mapperlyMapper.ToDto(_entity);
  }

  [Benchmark]
  public ComplexDto Mapster_Complex()
  {
    return _entity.Adapt<ComplexDto>();
  }

  [Benchmark(Baseline = true)]
  public ComplexDto Manual_Complex()
  {
    return new ComplexDto
    {
      Id = _entity.Id,
      Name = _entity.Name,
      Address = new AddressDto
      {
        Street = _entity.Address.Street,
        City = _entity.Address.City,
        ZipCode = _entity.Address.ZipCode,
        Country = _entity.Address.Country
      },
      Contact = new ContactDto
      {
        Email = _entity.Contact.Email,
        Phone = _entity.Contact.Phone
      },
      Items = _entity.Items.Select(item => new OrderItemDto
      {
        ProductId = item.ProductId,
        ProductName = item.ProductName,
        Quantity = item.Quantity,
        UnitPrice = item.UnitPrice
      }).ToList(),
      CreatedAt = _entity.CreatedAt,
      TotalAmount = _entity.TotalAmount
    };
  }
}
