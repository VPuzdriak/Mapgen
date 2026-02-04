# Mapgen

Mapgen is a compile-time, source-generator-based object mapping library for .NET that provides high performance, type safety, and excellent IDE support.

## Table of Contents

- [Getting Started](getting-started.md)
- [Core Features](core-features.md)
- [Advanced Usage](advanced-usage.md)
- [Best Practices](migration/best-practices.md)
- [Comparison with Other Libraries](comparison.md)
- [Migration Guides](migration/)
  - [From AutoMapper](migration/from-automapper.md)
  - [From Mapster](migration/from-mapster.md)
  - [From Mapperly](migration/from-mapperly.md)

## Why Mapgen?

### ✅ Compile-Time Code Generation
All mapping code is generated at compile time using source generators. This means:
- **Zero runtime overhead** - No reflection or dynamic code generation
- **Full IDE support** - Go-to-definition, find references, debugging
- **Early error detection** - Mapping issues caught during compilation

### ✅ Type Safety
- Strongly typed mapping configuration
- Compiler catches type mismatches
- Full IntelliSense support

### ✅ Performance
- As fast as hand-written mapping code
- No reflection penalty
- No runtime configuration overhead

### ✅ Flexibility
- Custom mapping expressions
- Method references for complex mappings
- Multi-parameter mapping support
- Collection mapping with custom logic
- Extension methods for convenience

### ✅ Simple API
- Intuitive fluent configuration
- Minimal boilerplate
- Clear, readable mapping definitions

## Quick Example

```csharp
using Mapgen.Analyzer;

// Define your mapper
[Mapper]
public partial class CarMapper
{
    // Declare the mapping method signature
    public partial CarDto ToCarDto(Car source);

    // Configure custom mappings
    public CarMapper()
    {
        MapMember(dest => dest.CountryOfOrigin, src => GetCountry(src.Make));
        MapMember(dest => dest.OwnerFullName, src => $"{src.Owner.FirstName} {src.Owner.LastName}");
    }

    private static string GetCountry(string make) => make switch
    {
        "Toyota" => "Japan",
        "Ford" => "USA",
        _ => "Unknown"
    };
}

// Use the mapper
var mapper = new CarMapper();
var carDto = mapper.ToCarDto(car);

// Or use the generated extension method
var carDto = car.ToCarDto();
```

## Installation

```bash
dotnet add package Mapgen.Analyzer --version 1.1.0
```

Or add to your `.csproj`:

```xml
<ItemGroup>
    <PackageReference Include="Mapgen.Analyzer" Version="1.1.0">
        <PrivateAssets>all</PrivateAssets>
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    </PackageReference>
</ItemGroup>
```

## Key Concepts

### 1. Mapper Classes
Mappers are partial classes decorated with the `[Mapper]` attribute. Mapgen generates the implementation in the other part of the partial class.

### 2. Mapping Methods
Declare partial methods with the signature of your mapping operation. Mapgen generates the implementation.

### 3. Configuration Methods
- `MapMember()` - Custom property mapping
- `MapCollection()` - Collection mapping with custom logic
- `IgnoreMember()` - Exclude properties from mapping
- `IncludeMappers()` - Compose mappers together

### 4. Extension Methods
Mapgen automatically generates extension methods for your source types, enabling clean, fluent mapping syntax.