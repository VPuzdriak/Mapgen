# Migration Guide: From AutoMapper to Mapgen

This guide will help you migrate your existing AutoMapper configurations to Mapgen.

## Table of Contents
- [Quick Start](#quick-start)
- [Basic Mapping](#basic-mapping)
- [Custom Member Mapping](#custom-member-mapping)
- [Nested Mappings](#nested-mappings)
- [Collection Mapping](#collection-mapping)
- [Conditional Mapping](#conditional-mapping)
- [Value Resolvers](#value-resolvers)
- [Dependency Injection](#dependency-injection)
- [Common Patterns](#common-patterns)

## Quick Start

### 1. Remove AutoMapper Packages

```bash
dotnet remove package AutoMapper
dotnet remove package AutoMapper.Extensions.Microsoft.DependencyInjection
```

### 2. Add Mapgen Package

```bash
dotnet add package Mapgen.Analyzer
```

Update your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="Mapgen.Analyzer" Version="1.0.1">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

### 3. Remove DI Configuration

```csharp
// REMOVE this from Startup.cs or Program.cs
services.AddAutoMapper(typeof(MappingProfile));
```

## Basic Mapping

### AutoMapper
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>();
        CreateMap<Product, ProductDto>();
        CreateMap<Order, OrderDto>();
    }
}

// Usage
public class UserService
{
    private readonly IMapper _mapper;
    
    public UserService(IMapper mapper)
    {
        _mapper = mapper;
    }
    
    public UserDto GetUser(User user)
    {
        return _mapper.Map<UserDto>(user);
    }
}
```

### Mapgen
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
}

[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);
}

[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);
}

// Usage
public class UserService
{
    public UserDto GetUser(User user)
    {
        return user.ToDto();  // Extension method
        // or: new UserMapper().ToDto(user)
    }
}
```

## Custom Member Mapping

### AutoMapper
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.FullName, 
                       opt => opt.MapFrom(src => $"{src.FirstName} {src.LastName}"))
            .ForMember(dest => dest.Age, 
                       opt => opt.MapFrom(src => CalculateAge(src.BirthDate)));
    }
    
    private int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

### Mapgen
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.FullName, 
                  user => $"{user.FirstName} {user.LastName}");
        MapMember(dto => dto.Age, 
                  user => CalculateAge(user.BirthDate));
    }
    
    private static int CalculateAge(DateTime birthDate)
    {
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

**Key Changes:**
- `ForMember()` → `MapMember()`
- `opt.MapFrom()` → direct lambda expression
- Method can be static in Mapgen

## Nested Mappings

### AutoMapper
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Department, DepartmentDto>();
        CreateMap<Employee, EmployeeDto>()
            .ForMember(dest => dest.Department, 
                       opt => opt.MapFrom(src => src.Department));
    }
}

// Usage
var employeeDto = _mapper.Map<EmployeeDto>(employee);
```

### Mapgen
```csharp
[Mapper]
public partial class DepartmentMapper
{
    public partial DepartmentDto ToDto(Department source);
}

[Mapper]
public partial class EmployeeMapper
{
    public partial EmployeeDto ToDto(Employee source);

    public EmployeeMapper()
    {
        MapMember(dto => dto.Department, 
                  emp => emp.Department.ToDto());
    }
}

// Usage
var employeeDto = employee.ToDto();
```

**Key Changes:**
- Explicitly call nested mapper extension methods
- More explicit, easier to trace

## Collection Mapping

### AutoMapper
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Items, 
                       opt => opt.MapFrom(src => src.Items));
        CreateMap<OrderItem, OrderItemDto>();
    }
}

// Usage
var orderDto = _mapper.Map<OrderDto>(order);
// Items are automatically mapped
```

### Mapgen - Simple Collection
```csharp
[Mapper]
public partial class OrderItemMapper
{
    public partial OrderItemDto ToDto(OrderItem source);
}

[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        // If collection properties match and have compatible types,
        // this may work automatically. Otherwise:
        MapCollection<OrderItemDto, OrderItem>(
            dest => dest.Items,
            item => item.ToDto()
        );
    }
}
```

### Mapgen - Collection with Context
```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source, Customer customer);

    public OrderMapper()
    {
        // Pass additional parameters to collection mappings
        MapCollection<OrderItemDto, OrderItem>(
            dest => dest.Items,
            (item, order, customer) => item.ToDto(customer)
        );
    }
}
```

## Conditional Mapping

### AutoMapper
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Status, 
                       opt => opt.MapFrom(src => 
                           src.IsActive ? "Active" : "Inactive"))
            .ForMember(dest => dest.Email, 
                       opt => opt.Condition(src => src.Email != null));
    }
}
```

### Mapgen
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.Status, 
                  user => user.IsActive ? "Active" : "Inactive");
        
        // For conditional mapping, use regular C# logic in lambda
        MapMember(dto => dto.Email, 
                  user => user.Email ?? "no-email@example.com");
    }
}
```

**Key Changes:**
- No special `Condition()` method needed
- Use standard C# conditional operators
- More straightforward and readable

## Value Resolvers

### AutoMapper
```csharp
public class OrderTotalResolver : IValueResolver<Order, OrderDto, decimal>
{
    public decimal Resolve(Order source, OrderDto destination, 
                          decimal destMember, ResolutionContext context)
    {
        return source.Items.Sum(i => i.Price * i.Quantity) + source.ShippingCost;
    }
}

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<Order, OrderDto>()
            .ForMember(dest => dest.Total, 
                       opt => opt.MapFrom<OrderTotalResolver>());
    }
}
```

### Mapgen
```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        MapMember(dto => dto.Total, CalculateTotal);
    }
    
    private static decimal CalculateTotal(Order order)
    {
        return order.Items.Sum(i => i.Price * i.Quantity) + order.ShippingCost;
    }
}
```

**Key Changes:**
- No need for resolver classes
- Use simple private methods
- Can be static for better performance
- Much less boilerplate

## Ignoring Members

### AutoMapper
```csharp
public class MappingProfile : Profile
{
    public MappingProfile()
    {
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.PasswordHash, opt => opt.Ignore())
            .ForMember(dest => dest.SecurityToken, opt => opt.Ignore());
    }
}
```

### Mapgen
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        IgnoreMember(dto => dto.PasswordHash);
        IgnoreMember(dto => dto.SecurityToken);
    }
}
```

**Key Changes:**
- `opt.Ignore()` → `IgnoreMember()`
- Simpler, more direct syntax

## Dependency Injection

### AutoMapper
```csharp
// Startup.cs
services.AddAutoMapper(typeof(MappingProfile));

// Controller
public class UserController : ControllerBase
{
    private readonly IMapper _mapper;
    private readonly IUserRepository _repository;
    
    public UserController(IMapper mapper, IUserRepository repository)
    {
        _mapper = mapper;
        _repository = repository;
    }
    
    [HttpGet("{id}")]
    public async Task<UserDto> GetUser(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return _mapper.Map<UserDto>(user);
    }
}
```

### Mapgen
```csharp
public class UserController : ControllerBase
{
    private readonly IUserRepository _repository;
    
    public UserController(IUserRepository repository)
    {
        _repository = repository;
    }
    
    [HttpGet("{id}")]
    public async Task<UserDto> GetUser(int id)
    {
        var user = await _repository.GetByIdAsync(id);
        return user.ToDto();  // Extension method
    }
}
```

**Key Changes:**
- DI is not needed
- Extension methods eliminate need for injecting mappers
- Simpler, less setup

## Common Patterns

### Pattern 1: ReverseMap

**AutoMapper:**
```csharp
CreateMap<User, UserDto>()
    .ReverseMap();
```

**Mapgen:**

Reverse mapping can be tricky to rely on implicit behavior, so it's best to create separate mappers for each direction:
```csharp
[Mapper]
public partial class UserToDtoMapper
{
    public partial UserDto ToDto(User source);
}

[Mapper]
public partial class UserDtoToEntityMapper
{
    public partial User ToEntity(UserDto source);
}
```

### Pattern 2: Multiple Profiles

**AutoMapper:**
```csharp
public class UserMappingProfile : Profile { /* ... */ }
public class OrderMappingProfile : Profile { /* ... */ }
public class ProductMappingProfile : Profile { /* ... */ }

services.AddAutoMapper(typeof(UserMappingProfile), 
                      typeof(OrderMappingProfile),
                      typeof(ProductMappingProfile));
```

**Mapgen:**

There is no concept of profiles in Mapgen. Just create separate mapper classes per mapping concern:
```csharp
// Just create separate mapper classes - no registration needed
[Mapper] public partial class UserMapper { /* ... */ }
[Mapper] public partial class OrderMapper { /* ... */ }
[Mapper] public partial class ProductMapper { /* ... */ }
```

### Pattern 3: ProjectTo (LINQ)

**AutoMapper:**
```csharp
var users = await context.Users
    .ProjectTo<UserDto>(_mapper.ConfigurationProvider)
    .ToListAsync();
```

**Mapgen:**

Mapgen doesn't support LINQ query translation (ProjectTo equivalent) at this moment. 

If you need to optimize database queries, use explicit select projections in your EF Core queries.

You can map after materialization since Mapgen does not support query translation. We will think for future versions about adding this feature.
```csharp
// Map after materialization
var users = await context.Users
    .ToListAsync();
var userDtos = users.Select(u => u.ToDto()).ToList();

// Or use AsEnumerable() for lazy evaluation
var userDtos = context.Users
    .AsEnumerable()
    .Select(u => u.ToDto());
```

## Migration Checklist

- [ ] Remove AutoMapper packages
- [ ] Add Mapgen.Analyzer package
- [ ] Remove `AddAutoMapper()` from DI configuration
- [ ] Convert Profile classes to Mapper classes
- [ ] Replace `CreateMap<,>()` with mapper class declarations
- [ ] Convert `ForMember()` to `MapMember()`
- [ ] Convert `opt.MapFrom()` to lambda expressions
- [ ] Convert `opt.Ignore()` to `IgnoreMember()`
- [ ] Replace resolver classes with private methods
- [ ] Update `_mapper.Map<T>()` calls to use extension methods or new mapper instances
- [ ] Remove IMapper dependencies from constructors (optional)
- [ ] Test all mappings
- [ ] Update unit tests

## Benefits After Migration

✅ **Performance**: No reflection, pure compile-time generation  
✅ **Debugging**: Easy to debug generated code  
✅ **IDE Support**: Full IntelliSense, go-to-definition  
✅ **Type Safety**: All errors caught at compile time  
✅ **Simplicity**: Less configuration, no DI setup required  
✅ **Transparency**: See exactly what code is generated  

## Getting Help

- [Core Features Documentation](../core-features.md)
- [Examples and Recipes](../examples.md)
- [Best Practices](../best-practices.md)

## Gradual Migration Strategy

For large projects, you can migrate gradually:

1. Keep AutoMapper installed
2. Create new Mapgen mappers for new features
3. Gradually convert existing AutoMapper profiles one at a time, starting with the most bottom-level ones
4. Remove AutoMapper once all profiles are converted

Both libraries can coexist during the transition period.

## Next Steps

- Review [Best Practices](best-practices.md)
- Explore [Advanced Usage](../advanced-usage.md)
- See [Core Features](../core-features.md)
