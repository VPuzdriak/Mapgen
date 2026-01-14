# Mapgen

[![Build And Test](https://github.com/VPuzdriak/Mapgen/actions/workflows/ci.yml/badge.svg)](https://github.com/VPuzdriak/Mapgen/actions/workflows/ci.yml)
[![NuGet](https://img.shields.io/nuget/v/Mapgen.Analyzer.svg)](https://www.nuget.org/packages/Mapgen.Analyzer)
[![Downloads](https://img.shields.io/nuget/dt/Mapgen.Analyzer.svg)](https://www.nuget.org/packages/Mapgen.Analyzer)
[![.NET](https://img.shields.io/badge/.NET-6.0+-blue.svg)](https://dotnet.microsoft.com/download)
[![License](https://img.shields.io/badge/license-MIT-green.svg)](LICENSE)

**A high-performance, compile-time object mapper for .NET using source generators.**

## 🚀 Quick Start

```bash
dotnet add package Mapgen.Analyzer
```

```csharp
using Mapgen.Analyzer;

// Define your mapper
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

// Use it
var userDto = user.ToDto();  // Extension method, auto-generated!
```

## ✨ Key Features

- **⚡ Zero Runtime Overhead** - All code generated at compile time
- **🛡️ Type Safe** - Full compile-time type checking
- **🔍 IDE Friendly** - Complete IntelliSense, go-to-definition, and debugging support
- **🎯 Explicit & Clear** - No magic, no reflection, readable generated code
- **🧩 Flexible** - Custom mappings, multi-parameter support, collection transformations
- **📦 Lightweight** - Minimal dependencies
- **🔧 Easy Testing** - Test mappers like regular classes

## 📚 Documentation

### Getting Started
- **[Documentation Home](docs/README.md)** - Overview and introduction
- **[Getting Started Guide](docs/getting-started.md)** - Installation and basic usage
- **[Core Features](docs/core-features.md)** - Detailed feature documentation

### Advanced Topics
- **[Advanced Usage](docs/advanced-usage.md)** - Complex mapping scenarios
- **[Comparison with Other Libraries](docs/comparison.md)** - How Mapgen compares to AutoMapper, Mapster, and Mapperly
- **[Architecture](docs/architecture.md)** - Internal architecture and source generator design

### Migration Guides
- **[From AutoMapper](docs/migration/from-automapper.md)** - Migrate from AutoMapper to Mapgen
- **[From Mapster](docs/migration/from-mapster.md)** - Migrate from Mapster to Mapgen
- **[From Mapperly](docs/migration/from-mapperly.md)** - Migrate from Mapperly to Mapgen

## 🎯 Why Mapgen?

### Compile-Time Generation
Unlike runtime mappers (AutoMapper, Mapster), Mapgen generates all mapping code during compilation. This means:
- **Faster execution** - No reflection or expression compilation at runtime
- **Earlier error detection** - Mapping issues caught during build
- **Better debugging** - Step through generated code like any other code

### Better Than Reflection-Based Mappers
```csharp
// Runtime mappers (AutoMapper)
var dto = _mapper.Map<UserDto>(user);  // ❌ Runtime resolution, opaque

// Mapgen
var dto = user.ToDto();  // ✅ Compile-time, transparent
```

### Cleaner Than Attribute-Heavy Approaches
```csharp
// Attribute-based (Mapperly)
[MapProperty(nameof(User.FirstName) + " + ' ' + " + nameof(User.LastName), 
             nameof(UserDto.FullName))]  // ❌ String-based, verbose

// Mapgen
MapMember(dto => dto.FullName, 
          user => $"{user.FirstName} {user.LastName}");  // ✅ Type-safe, clean
```

## 📖 Example Usage

### Basic Mapping
```csharp
[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);
}

// Auto-mapped: properties with matching names and compatible types
var productDto = product.ToDto();
```

### Custom Property Mapping
```csharp
[Mapper]
public partial class OrderMapper
{
    public partial OrderDto ToDto(Order source);

    public OrderMapper()
    {
        MapMember(dto => dto.Total, 
                  order => order.Items.Sum(i => i.Price * i.Quantity));
        
        MapMember(dto => dto.StatusText, GetStatusText);
    }
    
    private static string GetStatusText(Order order) => order.Status switch
    {
        OrderStatus.Pending => "Awaiting Payment",
        OrderStatus.Shipped => "In Transit",
        _ => "Unknown"
    };
}
```

### Multi-Parameter Mapping
```csharp
[Mapper]
public partial class InvoiceMapper
{
    public partial InvoiceDto ToDto(Invoice invoice, Customer customer);

    public InvoiceMapper()
    {
        MapMember(dto => dto.CustomerName, 
                  (invoice, customer) => customer.Name);
        MapMember(dto => dto.CustomerEmail, 
                  (_, customer) => customer.Email);
    }
}

// Usage
var dto = invoice.ToDto(customer);
```

### Collection Mapping
```csharp
[Mapper]
public partial class TeamMapper
{
    public partial TeamDto ToDto(Team source);

    public TeamMapper()
    {
        MapCollection<PlayerDto, Player>(
            dest => dest.Players,
            player => player.ToDto()
        );
    }
}
```

### Ignoring Properties
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

## 🎨 Real-World Example

```csharp
// Models
public class Car
{
    public string Make { get; init; }
    public string Model { get; init; }
    public int ReleaseYear { get; init; }
    public CarOwner Owner { get; init; }
}

public class CarDto
{
    public string Make { get; init; }
    public string Model { get; init; }
    public int ReleaseYear { get; init; }
    public string CountryOfOrigin { get; init; }
    public string OwnerFullName { get; init; }
    public DriverDto MainDriver { get; init; }
}

// Mapper
[Mapper]
public partial class CarMapper
{
    public partial CarDto ToCarDto(Car source, Driver driver);

    public CarMapper()
    {
        IncludeMappers([new CarOwnerMapper()]);
        MapMember(dest => dest.CountryOfOrigin, GetCountryName);
        MapMember(dest => dest.MainDriver, 
                  (_, drv) => drv.ToDriverDto());
    }

    private static string GetCountryName(Car src) => src.Make switch
    {
        "Toyota" => "Japan",
        "Ford" => "USA",
        "BMW" => "Germany",
        _ => "Unknown"
    };
}

// Usage
var carDto = car.ToCarDto(driver);
```

## 🔄 Migration from Other Libraries

Migrating to Mapgen is straightforward. We provide comprehensive guides:

- **[AutoMapper → Mapgen](docs/migration/from-automapper.md)** - Convert profiles to mappers
- **[Mapster → Mapgen](docs/migration/from-mapster.md)** - Replace conventions with explicit mappers
- **[Mapperly → Mapgen](docs/migration/from-mapperly.md)** - Switch from attributes to constructor config

## 🆚 Comparison

| Feature | Mapgen | AutoMapper | Mapster | Mapperly |
|---------|--------|------------|---------|----------|
| Approach | Source Generator | Runtime/Compiled | Runtime/Compiled | Source Generator |
| Performance | ⭐⭐⭐⭐⭐ | ⭐⭐⭐ | ⭐⭐⭐⭐ | ⭐⭐⭐⭐⭐ |
| IDE Support | ✅ Full | ❌ Limited | ❌ Limited | ✅ Full |
| Type Safety | ✅ Compile-time | ⚠️ Runtime | ⚠️ Runtime | ✅ Compile-time |
| Setup | ⭐ Simple | ⭐⭐⭐⭐ Complex | ⭐⭐ Moderate | ⭐ Simple |
| Debugging | ✅ Easy | ❌ Difficult | ⚠️ Moderate | ✅ Easy |

See the [full comparison](docs/comparison.md) for details.

## 🧪 Testing

Mappers are easy to test:

```csharp
[Fact]
public void ToDto_MapsPropertiesCorrectly()
{
    // Arrange
    var user = new User 
    { 
        FirstName = "John", 
        LastName = "Doe" 
    };
    var mapper = new UserMapper();
    
    // Act
    var dto = mapper.ToDto(user);
    
    // Assert
    Assert.Equal("John Doe", dto.FullName);
}
```

## 📦 Installation

### NuGet Package Manager
```bash
Install-Package Mapgen.Analyzer
```

### .NET CLI
```bash
dotnet add package Mapgen.Analyzer
```

### Package Reference
```xml
<ItemGroup>
    <PackageReference Include="Mapgen.Analyzer" Version="1.0.0" 
                      OutputItemType="Analyzer" 
                      ReferenceOutputAssembly="false"/>
</ItemGroup>
```

## 🛠️ Requirements

- .NET 6.0 or higher
- C# 10.0 or higher (for source generators)

## 📧 Support

If you encounter any issues or have questions, please open an issue on the repository.

## ⭐ Show Your Support

If you find Mapgen useful, please consider giving it a star! ⭐

---

**Happy Mapping! 🗺️**

