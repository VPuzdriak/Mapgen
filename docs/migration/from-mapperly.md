# Migration Guide: From Mapperly to Mapgen

This guide will help you migrate from Mapperly to Mapgen. Since both are source generator-based mapping libraries, the migration is relatively straightforward.

## Table of Contents
- [Quick Start](#quick-start)
- [Basic Mapping](#basic-mapping)
- [Custom Property Mapping](#custom-property-mapping)
- [Method-Based Mapping](#method-based-mapping)
- [Collection Mapping](#collection-mapping)
- [Nested Mappings](#nested-mappings)
- [Common Patterns](#common-patterns)

## Quick Start

### 1. Remove Mapperly Package

```bash
dotnet remove package Riok.Mapperly
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

## Basic Mapping

Both libraries use a similar approach with mapper classes and partial methods.

### Mapperly
```csharp
using Riok.Mapperly.Abstractions;

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
}

// Usage
var mapper = new UserMapper();
var userDto = mapper.ToDto(user);
```

### Mapgen
```csharp
using Mapgen.Analyzer;

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
}

// Usage - Option 1: Extension method (auto-generated)
var userDto = user.ToDto();

// Usage - Option 2: Mapper instance
var mapper = new UserMapper();
var userDto = mapper.ToDto(user);
```

**Key Difference:** Mapgen automatically generates extension methods for convenience.

## Custom Property Mapping

### Mapperly - Attributes
```csharp
using Riok.Mapperly.Abstractions;

[Mapper]
public partial class UserMapper
{
    [MapProperty(nameof(User.FirstName), nameof(UserDto.FirstName))]
    [MapProperty(nameof(User.LastName), nameof(UserDto.Surname))]
    public partial UserDto ToDto(User source);
}
```

### Mapgen - Constructor Configuration
```csharp
using Mapgen.Analyzer;

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.Surname, user => user.LastName);
    }
}
```

**Key Changes:**
- Attributes → Constructor configuration
- String property names → Lambda expressions
- Better IntelliSense and type safety

## Custom Expressions

### Mapperly
```csharp
[Mapper]
public partial class UserMapper
{
    [MapProperty(
        nameof(User.FirstName) + " + \" \" + " + nameof(User.LastName), 
        nameof(UserDto.FullName))]
    public partial UserDto ToDto(User source);
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
    }
}
```

**Key Changes:**
- String concatenation → Clean lambda expressions
- Compile-time checked
- Full C# syntax support

## Method-Based Mapping

### Mapperly
```csharp
[Mapper]
public partial class UserMapper
{
    [MapProperty(nameof(User.BirthDate), nameof(UserDto.Age), Use = nameof(CalculateAge))]
    public partial UserDto ToDto(User source);
    
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
        MapMember(dto => dto.Age, CalculateAge);
    }
    
    private static int CalculateAge(User user)
    {
        var today = DateTime.Today;
        var age = today.Year - user.BirthDate.Year;
        if (user.BirthDate.Date > today.AddYears(-age)) age--;
        return age;
    }
}
```

**Key Changes:**
- No `Use = nameof()` attribute needed
- Method reference directly in `MapMember()`
- Can be static for better performance

## Ignoring Properties

### Mapperly
```csharp
[Mapper]
public partial class UserMapper
{
    [MapperIgnoreTarget(nameof(UserDto.PasswordHash))]
    [MapperIgnoreTarget(nameof(UserDto.SecurityToken))]
    public partial UserDto ToDto(User source);
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
- `[MapperIgnoreTarget]` → `IgnoreMember()`
- Lambda expressions instead of string names
- Configuration in constructor, not attributes

## Collection Mapping

### Mapperly - Automatic
```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);
    
    // Collections with matching types are mapped automatically
}
```

### Mapgen - Automatic or Custom
```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        // Option 1: Automatic if types are compatible
        // No configuration needed
        
        // Option 2: Custom collection mapping
        MapCollection<OrderItemDto, OrderItem>(
            dest => dest.Items,
            item => item.ToDto()
        );
        
        // Option 3: Rely on existing mappers for item types
        IncludeMappers([new OrderMapper()]);
    }
}
```

### Mapperly - Custom Collection Mapping
```csharp
[Mapper]
public partial class OrderMapper
{
    [MapProperty(nameof(Order.OrderItems), nameof(OrderDto.Items))]
    public partial OrderDto ToDto(Order source);
    
    private OrderItemDto MapItem(OrderItem item)
    {
        return new OrderItemDto { /* ... */ };
    }
}
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
            src => src.OrderItems,
            item => item.ToDto()
        );
    }
}
```

## Multiple Source Parameters

### Mapperly
```csharp
[Mapper]
public partial class CarMapper
{
    [MapProperty(nameof(Car.Model), nameof(CarDto.Model))]
    public partial CarDto ToDto(Car car, [MapProperty(nameof(Driver.Name), nameof(CarDto.DriverName))] Driver driver);
}
```

### Mapgen
```csharp
[Mapper]
public partial class CarMapper
{
    public partial CarDto ToDto(Car car, Driver driver);

    public CarMapper()
    {
        MapMember(dto => dto.DriverName, 
                  (car, driver) => driver.Name);
    }
}

// Usage with extension method
var carDto = car.ToDto(driver);
```

**Key Changes:**
- Cleaner parameter syntax
- Lambda expressions with tuples
- Auto-generated extension methods with parameters

## Nested Object Mapping

### Mapperly
```csharp
[Mapper]
public partial class EmployeeMapper
{
    public partial EmployeeDto ToDto(Employee source);
    
    // Nested objects mapped automatically if mappings exist
    private partial DepartmentDto MapDepartment(Department source);
}
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
        // Option 1: Map via MapMember with existing mapper
        MapMember(dto => dto.Department, 
                  emp => emp.Department.ToDto());
        
        // Option 2: Include DepartmentMapper for automatic mapping
        IncludeMappers([new DepartmentMapper()]);
    }
}
```

**Key Changes:**
- Explicit mapper calls
- Easier to trace dependencies
- More obvious what's happening

## Mapper Configuration

### Mapperly - Per-Mapper Attributes
```csharp
[Mapper(EnumMappingStrategy = EnumMappingStrategy.ByName,
        PropertyNameMappingStrategy = PropertyNameMappingStrategy.CaseInsensitive)]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
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
        // Mapgen uses explicit configuration
        // Enum mapping handled automatically or explicitly
        // Property matching is case-sensitive by default
    }
}
```

**Key Difference:** Mapgen focuses on explicit configuration rather than global strategies.

## Constructor Mapping

### Mapperly
```csharp
[Mapper]
public partial class UserMapper
{
    [MapperConstructor]
    public partial UserDto ToDto(User source);
}

// Target has constructor with parameters
public class UserDto
{
    public UserDto(int id, string name)
    {
        Id = id;
        Name = name;
    }
}
```

### Mapgen

Mapgen currently focuses on property/init-based mapping. If you need constructor-based mapping, you might need to implement custom methods. We are looking into adding constructor mapping in future releases.

```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        // Properties mapped via initializer
    }
}
```

## Common Patterns

### Pattern 1: User-Implemented Methods

**Mapperly:**
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
    
    // User-implemented method
    private string GetFullName(User user) => $"{user.FirstName} {user.LastName}";
}
```

**Mapgen:**
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);

    public UserMapper()
    {
        MapMember(dto => dto.FullName, GetFullName);
    }
    
    private static string GetFullName(User user) => $"{user.FirstName} {user.LastName}";
}
```

### Pattern 2: Bidirectional Mapping

**Mapperly:**
```csharp
[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
    public partial User ToEntity(UserDto source);
}
```

**Mapgen:**

Mapgen demands separate mappers for each direction.

```csharp
// To Dto
[Mapper]
public partial class UserToDtoMapper
{
    public partial UserDto ToDto(User source);
}

// From Dto back to Entity
[Mapper]
public partial class UserDtoToEntityMapper
{
    public partial User ToEntity(UserDto source);
}
```

### Pattern 3: Static Mappers

**Mapperly:**
```csharp
[Mapper]
public static partial class UserMapper
{
    public static partial UserDto ToDto(User source);
}
```

**Mapgen:**
```csharp
// Mapgen requires instance mappers for configuration
// But you can use extension methods for static-like behavior

[Mapper]
public partial class UserMapper
{
    public partial UserDto ToDto(User source);
}

// Use the generated extension method
var dto = user.ToDto();  // Acts like a static method
```

## Configuration Style Comparison

### Mapperly - Attribute-Heavy
```csharp
[Mapper]
public partial class OrderMapper
{
    [MapProperty(nameof(Order.Id), nameof(OrderDto.OrderId))]
    [MapProperty(nameof(Order.CreatedAt), nameof(OrderDto.OrderDate))]
    [MapperIgnoreTarget(nameof(OrderDto.ProcessedAt))]
    [MapProperty(nameof(Order.TotalAmount), nameof(OrderDto.Total), Use = nameof(FormatAmount))]
    public partial OrderDto ToDto(Order source);
    
    private string FormatAmount(decimal amount) => $"${amount:F2}";
}
```

### Mapgen - Code-Based
```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        MapMember(dto => dto.OrderId, order => order.Id);
        MapMember(dto => dto.OrderDate, order => order.CreatedAt);
        IgnoreMember(dto => dto.ProcessedAt);
        MapMember(dto => dto.Total, FormatAmount);
    }
    
    private static string FormatAmount(Order order) => $"${order.TotalAmount:F2}";
}
```

**Key Changes:**
- Attributes → Constructor methods
- String names → Lambda expressions
- More readable and maintainable

## Migration Checklist

- [ ] Replace Mapperly package with Mapgen.Analyzer package
- [ ] Change using statement from `Riok.Mapperly.Abstractions` to `Mapgen.Analyzer`
- [ ] Convert `[MapProperty]` attributes to `MapMember()` calls in constructor
- [ ] Convert string property names to lambda expressions
- [ ] Replace `[MapperIgnoreTarget]` with `IgnoreMember()`
- [ ] Move property mapping from attributes to constructor
- [ ] Convert `Use = nameof()` to direct method references
- [ ] Update method signatures to accept source objects (not individual properties)
- [ ] Update usage to use extension methods (optional but recommended)
- [ ] Test all mappings
- [ ] Update unit tests

## Benefits After Migration

✅ **Cleaner Syntax**: Lambda expressions vs attributes  
✅ **Better Readability**: Configuration reads like code  
✅ **Extension Methods**: Automatically generated  
✅ **Less Verbose**: No repetitive attributes  
✅ **Better IntelliSense**: Full IDE support in lambdas  
✅ **Type Safety**: Compile-time checking throughout  

## Similarities (Advantages of Both)

✅ **Source Generators**: Both use compile-time generation  
✅ **Performance**: Both produce native-speed code  
✅ **Type Safety**: Both provide compile-time type checking  
✅ **Partial Classes**: Both use partial methods  
✅ **Debugging**: Generated code is easy to debug  

## Getting Help

- [Core Features Documentation](../core-features.md)
- [Examples and Recipes](../examples.md)
- [Best Practices](../best-practices.md)

## Gradual Migration

Both libraries can coexist during migration:

1. Keep Mapperly installed initially
2. Create new Mapgen mappers for new features
3. Convert Mapperly mappers one at a time
4. Remove Mapperly once complete

The similar architecture makes side-by-side usage straightforward.

## Next Steps

- Review [Best Practices](best-practices.md)
- Explore [Advanced Usage](../advanced-usage.md)
- See [Core Features](../core-features.md)
- Review [Best Practices](best-practices.md)
- Explore [Advanced Usage](../advanced-usage.md)
- See [Core Features](../core-features.md)
