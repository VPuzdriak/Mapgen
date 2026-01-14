using AutoMapper;

using Mapgen.Analyzer.Benchmarks.Models;

namespace Mapgen.Analyzer.Benchmarks.Mappers.AutoMapper;

public class BenchmarkProfile : Profile
{
  public BenchmarkProfile()
  {
    // Simple mapping
    CreateMap<SimpleEntity, SimpleDto>();

    // Complex mapping with nested objects
    CreateMap<Address, AddressDto>();
    CreateMap<Contact, ContactDto>();
    CreateMap<OrderItem, OrderItemDto>();
    CreateMap<ComplexEntity, ComplexDto>();

    // Immutable mapping with constructor
    CreateMap<ImmutableEntity, ImmutableDto>()
        .ConstructUsing(src => new ImmutableDto(
            src.Id,
            src.FirstName,
            src.LastName,
            src.Age,
            src.Email)
        {
          Address = src.Address
        });

    // Custom mapping with transformations
    CreateMap<CustomEntity, CustomDto>()
        .ForMember(dest => dest.FullName, opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
        .ForMember(dest => dest.AnnualSalary, opt => opt.MapFrom(src => src.Salary * 12))
        .ForMember(dest => dest.YearsOfService, opt => opt.MapFrom(src => CalculateYearsOfService(src.HireDate)))
        .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.IsActive ? "Active" : "Inactive"));
  }

  private static int CalculateYearsOfService(DateTime hireDate)
  {
    return (DateTime.Now - hireDate).Days / 365;
  }
}
