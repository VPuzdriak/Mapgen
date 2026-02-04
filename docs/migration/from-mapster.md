# Migration Guide: From Mapster to Mapgen

This guide will help you migrate from Mapster to Mapgen.

## Table of Contents
- [Quick Start](#quick-start)
- [Basic Mapping](#basic-mapping)
- [Custom Mapping](#custom-mapping)
- [Collection Mapping](#collection-mapping)
- [Configuration](#configuration)
- [Code Generation](#code-generation)
- [Common Patterns](#common-patterns)

## Quick Start

### 1. Remove Mapster Packages

```bash
dotnet remove package Mapster
dotnet remove package Mapster.DependencyInjection
dotnet remove package Mapster.Tool  # if using code generation
```

### 2. Add Mapgen Package

```bash
dotnet add package Mapgen.Analyzer
```

Update your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="Mapgen.Analyzer" Version="1.1.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

### 3. Remove Mapster Configuration

```csharp
// REMOVE this from Startup.cs or Program.cs
services.AddMapster();
```

## Basic Mapping

### Mapster - Convention-Based
```csharp
// No configuration needed for simple mappings
var userDto = user.Adapt<UserDto>();

// Or with explicit source
var userDto = user.Adapt<User, UserDto>();
```

### Mapgen
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
}

// Usage with extension method
var userDto = user.ToDto();

// Or with explicit mapper
var mapper = new UserMapper();
var userDto = mapper.ToDto(user);
```

**Key Changes:**
- Explicit mapper classes instead of convention
- `Adapt<T>()` → Extension method or mapper method
- Compile-time generation instead of runtime

## Custom Mapping

### Mapster - TypeAdapterConfig
```csharp
TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .Map(dest => dest.FullName, 
         src => $"{src.FirstName} {src.LastName}")
    .Map(dest => dest.Age, 
         src => CalculateAge(src.BirthDate));

// Usage
var userDto = user.Adapt<UserDto>();
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
        // Implementation
        var today = DateTime.Today;
        var age = today.Year - birthDate.Year;
        if (birthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}

// Usage
var userDto = user.ToDto();
```

**Key Changes:**
- `TypeAdapterConfig<,>.NewConfig()` → Mapper class constructor
- `.Map()` → `MapMember()`
- Helper methods defined in mapper class
- Configuration is local, not global

## Ignoring Members

### Mapster
```csharp
TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .Ignore(dest => dest.PasswordHash)
    .Ignore(dest => dest.SecurityToken);
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
- `.Ignore()` → `IgnoreMember()`
- Same lambda expression syntax

## Collection Mapping

### Mapster - Automatic
```csharp
public class Order
{
    public List<OrderItem> Items { get; set; }
}

public class OrderDto
{
    public List<OrderItemDto> Items { get; set; }
}

// Collections mapped automatically
var orderDto = order.Adapt<OrderDto>();
```

### Mapgen - Automatic or Custom
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
        // Option 1: Will work automatically if item types are same
        
        // Option 2: Explicit collection mapping
        MapCollection<OrderItemDto, OrderItem>(
            dest => dest.Items,
            item => item.ToDto()
            
        // Option 3: Use IncludeMappers if collection items are mapped by another mapper
        IncludeMappers([new OrderMapper()]);
    }
}
```

### Mapster - Custom Collection Mapping
```csharp
TypeAdapterConfig<Order, OrderDto>
    .NewConfig()
    .Map(dest => dest.Items, 
         src => src.Items.Select(i => i.Adapt<OrderItemDto>()).ToList());
```

### Mapgen - Custom Collection Mapping
```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        MapCollection<OrderItemDto, OrderItem>(
            dest => dest.Items,
            item => item.ToDto()
        );
    }
}
```

## Configuration Styles

### Mapster - Global Configuration
```csharp
// Global configuration (applies everywhere)
TypeAdapterConfig.GlobalSettings.Default
    .PreserveReference(true)
    .IgnoreNullValues(true);

TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .Map(dest => dest.FullName, 
         src => $"{src.FirstName} {src.LastName}");

// Used anywhere
var dto1 = user1.Adapt<UserDto>();
var dto2 = user2.Adapt<UserDto>();
```

### Mapgen - Local Configuration
```csharp
// Configuration is per mapper instance
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.FullName, 
                  user => $"{user.FirstName} {user.LastName}");
    }
}

// Each usage is explicit
var dto1 = user1.ToDto();
var dto2 = user2.ToDto();
```

**Key Difference:** Mapgen doesn't have global configuration. Each mapper is independent and explicit.

## Code Generation (Mapster.Tool)

### Mapster with Code Generation
```csharp
// Add attribute to models
[AdaptTo(typeof(UserDto))]
public class User
{
    public int Id { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
}

// Generated extension method
var userDto = user.Adapt<UserDto>();
```

### Mapgen
```csharp
// Define mapper class
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
}

// Generated extension method
var userDto = user.ToDto();
```

**Key Changes:**
- No attributes on models
- Mapper class instead of model attributes
- Cleaner separation of concerns

## Two-Way Mapping

### Mapster
```csharp
// Forward mapping
var userDto = user.Adapt<UserDto>();

// Reverse mapping (automatic)
var user = userDto.Adapt<User>();
```

### Mapgen
```csharp
[Mapper]
public partial class UserMapper
{
    // Forward mapping
    public partial UserDto ToDto(User source);
    
    // Reverse mapping is not supported 
    // Create new mapper class.
}

// Usage
var userDto = user.ToDto();
var user = userDto.ToEntity();
```

**Key Changes:**
- Reverse mapping must be explicitly declared
- More control over each direction
- Different method names for clarity

## Mapping with Parameters

### Mapster
```csharp
TypeAdapterConfig<Order, OrderDto>
    .NewConfig()
    .Map(dest => dest.CustomerName, 
         src => src.Customer.Name);

// Or using Adapt with parameters
var orderDto = order.Adapt<OrderDto>(new { Customer = customer });
```

### Mapgen
```csharp
[Mapper]
public partial class OrderMapper
{
    // Multiple parameters in method signature
    public partial OrderDto ToDto(Order source, Customer customer);

    public OrderMapper()
    {
        MapMember(dto => dto.CustomerName, 
                  (order, customer) => customer.Name);
    }
}

// Usage
var orderDto = order.ToDto(customer);
```

**Key Changes:**
- Parameters are explicit in method signature
- Type-safe, compile-time checked
- More obvious what data is needed

## Nested Mapping

### Mapster
```csharp
// Nested objects mapped automatically
public class Employee
{
    public Department Department { get; set; }
}

var employeeDto = employee.Adapt<EmployeeDto>();
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
        // Option 1: Use MapMember
        MapMember(dto => dto.Department, 
                  emp => emp.Department.ToDto());
        
        // Option 2: Use IncludeMappers
        IncludeMappers([new DepartmentMapper()]);
    }
}
```

**Key Changes:**
- Explicit nested mapper calls
- Easier to trace and debug
- Clear dependencies between mappers

## Conditional Mapping

### Mapster
```csharp
TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .Map(dest => dest.Email, 
         src => src.Email, 
         cond => cond.Email != null);
```

### Mapgen
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.Email, 
                  user => user.Email ?? "no-email@example.com");
    }
}
```

**Key Changes:**
- Use standard C# conditional operators
- More straightforward
- No special condition syntax

## Dependency Injection

### Mapster
```csharp
// Startup.cs
services.AddMapster();

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
public class UserService
{
    public UserDto GetUser(User user)
    {
        return user.ToDto();  // Extension method
    }
}
```

**Key Changes:**
- DI is not used in Mapgen
- Extension methods often eliminate need for DI
- Simpler setup

## Common Patterns

### Pattern 1: Query Projection (ProjectToType)

**Mapster:**
```csharp
var users = await context.Users
    .ProjectToType<UserDto>()
    .ToListAsync();
```

**Mapgen:**

Mapgen doesn't translate mappings to SQL. If you need query optimization, write explicit EF Core select projections.
We will look forward to adding this feature in future releases.

```csharp
// Map after materialization
var users = await context.Users.ToListAsync();
var userDtos = users.Select(u => u.ToDto()).ToList();

// Or for deferred execution
var userDtos = context.Users
    .AsEnumerable()
    .Select(u => u.ToDto());
```

### Pattern 2: After Mapping Action

**Mapster:**
```csharp
TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .AfterMapping((src, dest) => dest.MappedAt = DateTime.UtcNow);
```

**Mapgen:**

Mapgen does not have an explicit Before/After mapping hook.
We are investigating how often this pattern is used.
So far we can't recognize a strong use case that can't be implemented in other ways.

### Pattern 3: Before Mapping

**Mapster:**
```csharp
TypeAdapterConfig<User, UserDto>
    .NewConfig()
    .BeforeMapping((src, dest) => ValidateUser(src));
```

**Mapgen:**

Mapgen does not have an explicit Before/After mapping hook.
We are investigating how often this pattern is used.
So far we can't recognize a strong use case that can't be implemented in other ways.

## Migration Checklist

- [ ] Remove Mapster packages
- [ ] Add Mapgen.Analyzer package
- [ ] Remove `AddMapster()` from DI configuration
- [ ] Create mapper classes for each mapping
- [ ] Convert `TypeAdapterConfig<,>` to mapper constructors
- [ ] Replace `.Map()` with `MapMember()`
- [ ] Replace `.Ignore()` with `IgnoreMember()`
- [ ] Update `Adapt<T>()` calls to use extension methods
- [ ] Handle collection mappings explicitly if needed
- [ ] Update nested object mappings
- [ ] Remove model attributes if using Mapster.Tool
- [ ] Test all mappings
- [ ] Update unit tests

## Benefits After Migration

✅ **Compile-Time Safety**: All mappings verified at compile time  
✅ **Better IDE Support**: Full IntelliSense and debugging  
✅ **Explicit Configuration**: No global state or conventions  
✅ **Type Safety**: Strong typing for all parameters  
✅ **Transparency**: See generated code  
✅ **Performance**: Identical to hand-written code  

## Gradual Migration

Both libraries can coexist:

1. Keep Mapster installed initially
2. Create Mapgen mappers for new features
3. Gradually convert Mapster configs to Mapgen
4. Remove Mapster when conversion is complete

## Next Steps

- Review [Best Practices](best-practices.md)
- Explore [Advanced Usage](../advanced-usage.md)
- See [Core Features](../core-features.md)
