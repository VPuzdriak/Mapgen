# Architecture

This document describes the internal architecture of Mapgen's source generator system.

## Table of Contents
- [Overview](#overview)
- [High-Level Architecture](#high-level-architecture)
- [Component Structure](#component-structure)
- [Processing Pipeline](#processing-pipeline)
- [Key Components](#key-components)
- [Mapping Strategies](#mapping-strategies)
- [Code Generation](#code-generation)
- [Diagnostics System](#diagnostics-system)

## Overview

Mapgen is a compile-time source generator that analyzes mapper class definitions and generates efficient mapping code. It leverages the Roslyn compiler platform to:

1. Detect classes decorated with `[Mapper]` attribute
2. Parse mapping configuration from constructor calls
3. Generate type-safe mapping implementations
4. Generate extension methods for convenient mapper usage
5. Report compilation errors for invalid configurations

## High-Level Architecture

```
┌─────────────────────────────────────────────────────────────┐
│                    Roslyn Compiler                          │
└─────────────┬───────────────────────────────────┬───────────┘
              │                                   │
              ▼                                   ▼
   ┌──────────────────────┐          ┌──────────────────────┐
   │ MappingGenerator     │          │ MappingExtensions     │
   │ (IIncremental        │          │ Generator             │
   │  Generator)          │          │ (IIncremental         │
   └──────────┬───────────┘          │  Generator)           │
              │                      └──────────┬────────────┘
              ▼                                 │
   ┌──────────────────────┐                     │
   │ MapperMethod         │                     │
   │ Transformer          │                     │
   └──────────┬───────────┘                     │
              │                                 │
              ▼                                 │
   ┌──────────────────────┐                     │
   │ Mapping Strategies   │                     │
   │ • DirectMapping      │                     │
   │ • CustomMapping      │                     │
   │ • CollectionMapping  │                     │
   │ • IgnoreMapping      │                     │
   └──────────┬───────────┘                     │
              │                                 │
              ▼                                 ▼
   ┌──────────────────────┐          ┌──────────────────────┐
   │ MapperTemplate       │          │ ExtensionsTemplate   │
   │ Engine               │          │ Engine               │
   └──────────┬───────────┘          └──────────┬───────────┘
              │                                 │
              └───────────┬─────────────────────┘
                          ▼
              ┌─────────────────────┐
              │ Generated C# Code   │
              └─────────────────────┘
```

## Component Structure

### Core Package

#### Mapgen.Analyzer
The single package that contains both the source generator implementation and generates the public API:

**Generated Public API** (in `Mapgen.Analyzer` namespace):
- `MapperAttribute` - Marks a class as a mapper (auto-generated at compile time)
- Other attributes and abstractions as needed

**Source Generator Implementation:**

```
Mapgen.Analyzer/
├── Mapper/                      # Main mapper generation logic
│   ├── AttributeGenerator.cs   # Generates MapperAttribute
│   ├── MappingGenerator.cs     # Entry point, IIncrementalGenerator
│   ├── MapperMethodTransformer.cs # Orchestrates mapping strategies
│   ├── MappingParser.cs        # Parses included mappers
│   ├── MapperTemplateEngine.cs # Generates mapper code
│   ├── Constants.cs            # Shared constants
│   ├── Metadata/               # Metadata models
│   │   ├── MappingConfigurationMetadata.cs
│   │   ├── MapperMethodMetadata.cs
│   │   ├── MapperMethodParameter.cs
│   │   └── IncludedMapperInfo.cs
│   ├── MappingDescriptors/     # Mapping result models
│   │   ├── BaseMappingDescriptor.cs
│   │   ├── MappingDescriptor.cs
│   │   ├── IgnoredPropertyDescriptor.cs
│   │   └── DiagnosedPropertyDescriptor.cs
│   ├── Strategies/             # Mapping strategies
│   │   ├── DirectMappingStrategy.cs
│   │   ├── CustomMappingStrategy.cs
│   │   ├── CollectionMappingStrategy.cs
│   │   └── IgnoreMappingStrategy.cs
│   └── Diagnostics/            # Error reporting
│       ├── MapperDiagnostic.cs
│       └── MapperDiagnosticsReporter.cs
└── Extensions/                 # Extension method generation
    ├── MappingExtensionsGenerator.cs
    ├── MappingExtensionsTemplateEngine.cs
    ├── MapperExtensionsMetadata.cs
    ├── ExtensionMethodInfo.cs
    └── ParameterInfo.cs
```

## Processing Pipeline

### 1. Discovery Phase
```
User Code with [Mapper] attribute
          ↓
ForAttributeWithMetadataName()
          ↓
Predicate() - Always true (accept all)
          ↓
Transform() - Extract metadata
```

### 2. Transformation Phase
```
GeneratorAttributeSyntaxContext
          ↓
Extract mapper class info
          ↓
MapperMethodTransformer.Transform()
          ↓
Apply mapping strategies in order:
  1. ParseIncludedMappers
  2. ParseEnumDeclarations (MapEnum calls)
  3. AddIgnoreMappings
  4. AddCustomMappings
  5. AddCollectionMappings
  6. AddDirectMappings (includes automatic enum detection)
  7. AddUnmappedPropertyDiagnostics
          ↓
Process Constructor Info (if UseConstructor)
  - Detect enum parameters
  - Generate enum helper methods
          ↓
MapperMethodMetadata (with mappings, enum helpers & diagnostics)
```

### 3. Generation Phase
```
MappingConfigurationMetadata
          ↓
MapperDiagnosticsReporter.Report()
          ↓
MapperTemplateEngine.GenerateMapperClass()
  - Generate mapper fields
  - Generate mapping methods
  - Generate enum helper methods (from EnumMappingMethods)
  - Generate configuration helper methods (MapMember, etc.)
          ↓
Generated source code added to compilation
```

## Key Components

### MappingGenerator

**Purpose**: Entry point for mapper code generation

**Responsibilities**:
- Registers with Roslyn as an incremental source generator
- Detects classes with `[Mapper]` attribute
- Orchestrates the transformation pipeline
- Generates source code via template engine
- Reports diagnostics

**Key Methods**:
- `Initialize()` - Sets up the incremental generator pipeline
- `Transform()` - Converts syntax to metadata (extracts usings, mapper info, method details)
- `Generate()` - Produces final source code

### MapperMethodTransformer

**Purpose**: Orchestrates the mapping strategy pipeline

**Responsibilities**:
- Applies all mapping strategies in correct order
- Builds `MapperMethodMetadata` with all mappings
- Ensures ignored properties override other strategies
- Ensures custom mappings override automatic mappings
- Collects diagnostics from each strategy

**Strategy Order** (important for precedence):
1. **Included Mappers** - Parse mapper dependencies
2. **Ignore Mappings** - Properties explicitly ignored
3. **Custom Mappings** - Explicitly configured via `MapMember()`
4. **Collection Mappings** - Explicitly configured via `MapCollection()`
5. **Direct Mappings** - Automatic property-to-property mapping
6. **Diagnostics** - Detect unmapped properties

### MappingParser

**Purpose**: Parse mapper configuration from constructor

**Responsibilities**:
- Find `IncludeMappers()` calls in constructor
- Extract included mapper types from collection expressions
- Build `IncludedMapperInfo` for each dependency
- Find `MapEnum<TSource, TDest>()` declarations
- Extract enum type arguments and build `EnumMappingDeclaration` for each

**Example (IncludeMappers)**:
```csharp
public CarOwnerMapper() {
    IncludeMappers([new CarMapper(), new DriverMapper()]);
}
```

**Example (MapEnum)**:
```csharp
public OrderMapper() {
    MapEnum<CustomerStatus, CustomerStatusDto>();
    MapEnum<OrderPriority, OrderPriorityDto>();
}
```

**MapEnum Processing**:
1. Searches for `MapEnum` invocations in constructor
2. Validates exactly 2 type arguments: `<TSourceEnum, TDestEnum>`
3. Extracts type symbols for both enums
4. Creates `EnumMappingDeclaration` with source type, destination type, and location
5. Stores declarations in `MapperMethodMetadata` for later processing

### MapperTemplateEngine

**Purpose**: Generate the final mapper implementation

**Responsibilities**:
- Generate using directives
- Generate mapper fields for included mappers
- Generate partial method implementations
- Generate helper methods (`MapMember`, `MapCollection`, `IgnoreMember`, `IncludeMappers`)
- Use string templates with placeholder replacement

**Output Structure**:
```csharp
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
// ... user usings ...

namespace MyNamespace {
  public partial class CarMapper {
    // Fields for included mappers
    private readonly CarOwnerMapper _carOwnerMapper = new CarOwnerMapper();
    
    // Partial method implementation
    public partial CarDto ToDto(Car car) {
      return new CarDto {
        Id = car.Id,
        Model = car.Model,
        Owner = _carOwnerMapper.ToDto(car.Owner),
        // ... mappings ...
      };
    }
    
    // Helper methods (configuration API)
    private void MapMember<TDestinationMember>(...) { }
    private void MapCollection<TDestinationItem, TSourceItem>(...) { }
    private void IgnoreMember<TDestinationMember>(...) { }
    private void IncludeMappers(object[] mappers) { }
  }
}
```

## Mapping Strategies

Each strategy handles a specific type of property mapping. They are applied in order, with earlier strategies taking precedence.

### DirectMappingStrategy

**Purpose**: Automatic property-to-property mapping

**When Applied**: For properties with matching names that haven't been handled by other strategies

**Logic**:
1. Check if source and destination property types match exactly
2. Check for enum-to-enum compatibility (all source members exist in destination)
3. Check for nullable/non-nullable mismatches (reports diagnostic)
4. Check for implicit type conversions (e.g., `int` → `long`)
5. Check for collection mappings with compatible element types
6. Check for included mapper that can map between types
7. If no valid mapping found, report type mismatch diagnostic

**Example**:
```csharp
// Auto-mapped (exact match)
car.Id → carDto.Id

// Auto-mapped (enum with matching members)
order.Status (OrderStatus) → orderDto.Status (OrderStatusDto)
// Generates: Status = MapToOrderStatusDto(order.Status)

// Auto-mapped (implicit conversion)
car.Year (short) → carDto.Year (int)

// Auto-mapped (via included mapper)
car.Owner (CarOwner) → carDto.Owner (CarOwnerDto)
// Uses _carOwnerMapper.ToDto(car.Owner)
```

**Enum Mapping Details**:

Mapgen provides comprehensive enum mapping support with compile-time validation:

**Automatic Detection**:
- Detects when source and destination properties are both enums
- Analyzes enum members to ensure compatibility
- Generates helper methods automatically when needed

**Name-Based Mapping**:
- Maps enums **by member NAME** using switch expressions, not by numeric value
- Ensures semantic correctness (prevents mapping `Status.Active = 1` to `StatusDto.Pending = 1`)
- Case-sensitive member name matching

**Helper Method Generation**:
```csharp
// For property: OrderStatus → OrderStatusDto
private static OrderStatusDto MapToOrderStatusDto(OrderStatus value) 
    => value switch
    {
        OrderStatus.Pending => OrderStatusDto.Pending,
        OrderStatus.Shipped => OrderStatusDto.Shipped,
        OrderStatus.Delivered => OrderStatusDto.Delivered,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unexpected enum value")
    };
```

**Nullable Support**:
- Automatically generates nullable overloads
- `OrderStatus?` → `OrderStatusDto?` handled transparently
```csharp
private static OrderStatusDto? MapToOrderStatusDto(OrderStatus? value) 
    => value.HasValue ? MapToOrderStatusDto(value.Value) : null;
```

**Collection Support**:
- Enum collections automatically mapped: `List<OrderStatus>` → `List<OrderStatusDto>`
- Uses LINQ: `source.StatusHistory.Select(MapToOrderStatusDto).ToList()`

**Constructor Parameter Support**:
- Enum constructor parameters automatically mapped
- Helper methods available for both automatic and explicit `UseConstructor()` scenarios

**Validation**:
- Reports `MAPPER012` diagnostic if source has members not in destination
- Requires all source enum values to have corresponding destination values
- Destination enum can have extra members (not an error)

**MapEnum Declarations**:
- `MapEnum<TSource, TDest>()` explicitly declares enum mappings
- Used when enum doesn't match any source property name
- Makes helper methods available for use in `UseConstructor()` or `MapMember()`
- Processes same validation as automatic enum detection

### CustomMappingStrategy

**Purpose**: Parse and apply explicit mappings defined via `MapMember()`

**When Applied**: During strategy pipeline, before direct mappings

**Logic**:
1. Find all `MapMember()` invocations in constructor
2. Extract destination property name from first lambda
3. Extract source expression from second lambda
4. Validate lambda is expression-bodied (not block)
5. Create `MappingDescriptor` with source expression string

**Example**:
```csharp
// Constructor
MapMember(dto => dto.FullName, car => car.Owner.FirstName + " " + car.Owner.LastName);

// Generated mapping
FullName = car.Owner.FirstName + " " + car.Owner.LastName,
```

**Expression Handling**:
- Supports property access: `car => car.Name`
- Supports method calls: `car => car.Name.ToUpper()`
- Supports arithmetic: `car => car.Price * 1.1`
- Supports string interpolation: `car => $"{car.Make} {car.Model}"`
- Supports multiple parameters: `(car, driver) => car.ToDto(driver)`

**Validation**:
- Rejects lambda blocks (reports `MAPPER002` diagnostic)
- Preserves exact expression syntax from source code

### CollectionMappingStrategy

**Purpose**: Parse and apply collection mappings defined via `MapCollection()`

**When Applied**: After custom mappings, before direct mappings

**Logic**:
1. Find all `MapCollection()` invocations in constructor
2. Handle two overload types:
   - **Single expression**: `MapCollection(dto => dto.Cars, car => car.ToDto())`
   - **Two expressions**: `MapCollection(dto => dto.Cars, owner => owner.Cars, car => car.ToDto())`
3. Extract destination and optional source collection properties
4. Extract item transformation expression
5. Generate LINQ `.Select()` call with transformation

**Example (Single Expression)**:
```csharp
// Constructor
MapCollection(dto => dto.Cars, car => car.ToDto(driver));

// Generated mapping
Cars = garage.Cars.Select(car => car.ToDto(driver)).ToList(),
```

**Example (Two Expressions)**:
```csharp
// Constructor
MapCollection(dto => dto.CarsDto, garage => garage.Cars, (car, garage, driver) => car.ToDto(driver));

// Generated mapping
CarsDto = garage.Cars.Select(car => car.ToDto(garage, driver)).ToList(),
```

**Collection Type Handling**:
- Detects `IEnumerable<T>`, `ICollection<T>`, `IList<T>`, `List<T>`, etc.
- Generates `.ToList()` for list types
- Generates `.ToArray()` for array types
- Generates `.ToImmutableList()` for immutable collections

### IgnoreMappingStrategy

**Purpose**: Mark properties as intentionally unmapped

**When Applied**: After included mappers, before other strategies

**Logic**:
1. Find all `IgnoreMember()` invocations in constructor
2. Extract destination property name from lambda
3. Create `IgnoredPropertyDescriptor` to suppress unmapped property diagnostic

**Example**:
```csharp
// Constructor
IgnoreMember(dto => dto.CalculatedField);

// Result: No mapping generated, no diagnostic reported
```

## Enum Mapping Pipeline

Enum mapping in Mapgen involves multiple stages from detection to code generation:

### 1. Enum Declaration Detection

**Sources of Enum Mappings**:

**A. Explicit MapEnum Declarations**:
```csharp
public OrderMapper() {
    MapEnum<CustomerStatus, CustomerStatusDto>();
}
```
- Parsed in `MappingParser.ParseMapEnumDeclarations()`
- Creates `EnumMappingDeclaration` with source/dest types
- Stored in `MapperMethodMetadata.EnumMappingDeclarations`

**B. Automatic Constructor Detection**:
```csharp
// Constructor parameter requires enum mapping
public OrderDto(int id, OrderPriorityDto priority) { }
```
- Detected in `MapperMethodTransformer` during constructor analysis
- When constructor parameter type differs from source property type
- Both must be enums for automatic helper generation

**C. Automatic Property Detection**:
```csharp
// Property-to-property enum mapping
public class OrderDto {
    public OrderStatusDto Status { get; set; }
}
```
- Detected in `DirectMappingStrategy`
- When source and destination properties are both enums
- Property names must match (case-insensitive)

### 2. Enum Compatibility Validation

**Validation Process** (in `EnumMappingHelpers`):
1. Extract all member names from source enum
2. Extract all member names from destination enum
3. Check if all source members exist in destination
4. **Allowed**: Destination has extra members not in source
5. **Error**: Source has members not in destination → `MAPPER012`

**Example Validation**:
```csharp
// Source enum
public enum OrderStatus { Pending, Shipped, Delivered }

// ✅ Valid - destination has extra member
public enum OrderStatusDto { Pending, Shipped, Delivered, Returned }

// ❌ Invalid - source has "Cancelled" not in destination
public enum OrderStatusDto2 { Pending, Shipped, Delivered }
```

### 3. Helper Method Generation

**EnumMappingMethodInfo Creation**:
- For each enum pair, create `EnumMappingMethodInfo`
- Method name: `MapTo{DestinationEnumName}`
- Store in `MapperMethodMetadata.EnumMappingMethods`

**Code Generation** (in `MapperTemplateEngine`):
```csharp
private string GenerateEnumMappingMethod(EnumMappingMethodInfo methodInfo)
{
    // Generate non-nullable version
    private static DestEnum MapToDestEnum(SourceEnum value) 
        => value switch
        {
            SourceEnum.Member1 => DestEnum.Member1,
            SourceEnum.Member2 => DestEnum.Member2,
            _ => throw new ArgumentOutOfRangeException(...)
        };
    
    // Generate nullable version
    private static DestEnum? MapToDestEnum(SourceEnum? value) 
        => value.HasValue ? MapToDestEnum(value.Value) : null;
}
```

### 4. Helper Method Usage

**In Constructor Mapping**:
```csharp
return new OrderDto(
    order.Id,
    MapToOrderPriorityDto(order.Priority)  // Helper method call
);
```

**In Property Mapping**:
```csharp
return new OrderDto {
    Status = MapToOrderStatusDto(order.Status),  // Helper method call
    // ...
};
```

**In Collection Mapping**:
```csharp
StatusHistory = order.StatusHistory
    .Select(MapToOrderStatusDto)  // Helper as method group
    .ToList()
```

**In Custom Expressions**:
```csharp
MapMember(dto => dto.StatusDisplay, 
    order => $"Status: {MapToOrderStatusDto(order.Status)}");
```

### 5. Deduplication

Mapgen ensures each enum pair generates exactly one set of helper methods:

1. Collect all enum pairs from explicit declarations
2. Collect all enum pairs from automatic detection
3. Deduplicate by source/destination type combination
4. Generate one helper method set per unique pair

**Example**:
```csharp
public OrderMapper() {
    // Explicit declaration
    MapEnum<OrderStatus, OrderStatusDto>();
    
    UseConstructor(
        (order, customer) => order.Id,
        // Would auto-detect OrderStatus → OrderStatusDto
        (order, customer) => MapToOrderStatusDto(order.Status)
    );
}
// Result: Only ONE set of MapToOrderStatusDto methods generated
```

### 6. Error Reporting

**MAPPER012: Incompatible Enum Mapping**:
- Reported when source enum has members not in destination
- Includes list of missing members in error message
- Suggests using `MapMember()` for custom handling

**Error Message Format**:
```
error MAPPER012: Enum type "OrderStatus" cannot be automatically mapped 
to "OrderStatusDto" because source enum has members not present in 
destination: "Cancelled". Remove the MapEnum() call and make mapping 
manually or add missing members to the destination enum.
```

### Pipeline Summary

```
┌─────────────────────────────────────────────────────────────┐
│           1. Detection Phase                                │
│  ┌──────────────┐  ┌──────────────┐  ┌─────────────────┐  │
│  │ MapEnum()    │  │ Constructor  │  │ Property Names  │  │
│  │ Declarations │  │ Parameters   │  │ Match           │  │
│  └──────┬───────┘  └──────┬───────┘  └────────┬────────┘  │
│         └──────────────────┼────────────────────┘           │
└────────────────────────────┼────────────────────────────────┘
                             ▼
                ┌────────────────────────┐
                │  2. Validation Phase   │
                │  - Check members       │
                │  - Report MAPPER012    │
                └───────────┬────────────┘
                            ▼
                ┌────────────────────────┐
                │  3. Generation Phase   │
                │  - Create method info  │
                │  - Deduplicate         │
                └───────────┬────────────┘
                            ▼
                ┌────────────────────────┐
                │  4. Code Gen Phase     │
                │  - Generate helpers    │
                │  - Non-nullable        │
                │  - Nullable            │
                └───────────┬────────────┘
                            ▼
                ┌────────────────────────┐
                │  5. Usage in Mappings  │
                │  - Constructor args    │
                │  - Property mapping    │
                │  - Collections         │
                │  - Custom expressions  │
                └────────────────────────┘
```

## Code Generation

### Two Generators

Mapgen uses two separate incremental generators:

#### 1. MappingGenerator
Generates the mapper implementation:
- File name: `{MapperName}.g.cs`
- Contains: Partial method implementation, helper methods

#### 2. MappingExtensionsGenerator
Generates extension methods:
- File name: `{MapperName}Extensions.g.cs`
- Contains: Extension methods for convenient usage

**Example Extension Method**:
```csharp
public static class CarMapperExtensions {
  private static readonly CarMapper _carMapper = new CarMapper();
  
  public static CarDto ToDto(this Car source) {
    return _carMapper.ToDto(source);
  }
}
```

### Template-Based Generation

Both generators use template engines with string replacement:

```csharp
var template = """
    namespace {{Namespace}} {
      public partial class {{ClassName}} {
        {{Methods}}
      }
    }
    """;

var result = new StringBuilder(template)
    .Replace("{{Namespace}}", namespaceName)
    .Replace("{{ClassName}}", className)
    .Replace("{{Methods}}", methods)
    .ToString();
```

### Helper Method Generation

The generator creates helper methods that serve as the configuration API. These methods are never actually executed—they exist only for configuration parsing:

```csharp
private void MapMember<TDestinationMember>(
  Expression<Func<CarDto, TDestinationMember>> destinationMember,
  Func<Car, TDestinationMember> sourceFunc) {
  // Mapgen will use this method as mapping configuration.
}

private void MapEnum<TSourceEnum, TDestinationEnum>()
  where TSourceEnum : struct, Enum
  where TDestinationEnum : struct, Enum {
  // Mapgen will use this method to generate enum mapping methods.
}
```

**Multiple Overloads**: The generator creates overloads for different parameter counts:
```csharp
// 1 parameter
MapMember(dto => dto.Name, car => car.Model)

// 2 parameters
MapMember(dto => dto.Name, (car, driver) => car.Model + " driven by " + driver.Name)
```

**Enum Mapping Helper Methods**: The generator creates static helper methods for enum conversions:
```csharp
// Non-nullable overload
private static OrderStatusDto MapToOrderStatusDto(OrderStatus value) 
    => value switch
    {
        OrderStatus.Pending => OrderStatusDto.Pending,
        OrderStatus.Shipped => OrderStatusDto.Shipped,
        OrderStatus.Delivered => OrderStatusDto.Delivered,
        _ => throw new ArgumentOutOfRangeException(nameof(value), value, "Unexpected enum value")
    };

// Nullable overload
private static OrderStatusDto? MapToOrderStatusDto(OrderStatus? value) 
    => value.HasValue ? MapToOrderStatusDto(value.Value) : null;
```

**When Enum Helpers are Generated**:
1. **Automatic**: When constructor parameters or properties require enum mapping
2. **Explicit**: When `MapEnum<TSource, TDest>()` is called in constructor
3. **Naming**: Helper methods use pattern `MapTo{DestinationEnumName}`

## Diagnostics System

### MapperDiagnostic

Represents a compilation error or warning with:
- **Id**: Diagnostic code (e.g., `MAPPER001`)
- **Title**: Short description
- **MessageFormat**: Template for error message
- **Severity**: Error, Warning, or Info
- **Location**: Source code location for error highlighting
- **MessageArgs**: Arguments for message formatting

### Common Diagnostics

| Code | Title | Severity | Description                                                |
|------|-------|----------|------------------------------------------------------------|
| MAPPER001 | Missing property mapping | Error | Destination has a a property which was not mapped          |
| MAPPER002 | Lambda block not supported | Error | `MapMember()` uses block lambda instead of expression      |
| MAPPER003 | Multiple mapping methods | Error | Mapper has more than one partial method                    |
| MAPPER004 | Type mismatch | Error | Source/destination types incompatible, no conversion exists |
| MAPPER005 | Nullable mismatch | Warning | Mapping nullable to non-nullable (potential null reference) |
| MAPPER006 | Required member cannot be ignored | Error | Member with `required` keyword cannot be ignored |
| MAPPER007 | Parameterized constructor required | Error | Destination requires constructor but none selected |
| MAPPER008 | Ambiguous constructor selection | Error | Multiple constructors available, explicit selection needed |
| MAPPER009 | UseEmptyConstructor not possible | Error | Destination has no parameterless constructor |
| MAPPER010 | Mapper constructor with parameters | Error | Mapper constructor must be parameterless |
| MAPPER011 | Invalid constructor statement | Error | Mapper constructor contains invalid statements |
| MAPPER012 | Incompatible enum mapping | Error | Source enum has members not present in destination enum (applies to properties, constructor parameters, and collections) |

### MapperDiagnosticsReporter

**Purpose**: Report all diagnostics to Roslyn compiler

**Process**:
1. Iterate through class-level diagnostics
2. Iterate through method-level diagnostics
3. Create `DiagnosticDescriptor` for each
4. Report to `SourceProductionContext`
5. Errors appear in IDE and block compilation

## Metadata Models

### MappingConfigurationMetadata
Top-level metadata for entire mapper class:
- Usings (namespace imports)
- Mapper namespace and name
- Method metadata
- Class-level diagnostics

### MapperMethodMetadata
Metadata for single mapping method:
- Method symbol (Roslyn `IMethodSymbol`)
- Return type and name
- Parameters
- Mappings (list of descriptors)
- Diagnostics (errors/warnings)
- Included mappers
- Enum mapping declarations
- Enum mapping methods (generated helper methods)

### EnumMappingDeclaration
Represents an explicit `MapEnum<TSource, TDest>()` call in mapper constructor:
- **SourceEnumType**: Source enum type symbol
- **DestEnumType**: Destination enum type symbol  
- **Location**: Source code location for error reporting

Used to generate enum mapping helper methods that can be used in constructor parameters and custom mappings.

### EnumMappingMethodInfo
Metadata for a generated enum mapping helper method:
- **SourceEnumType**: Source enum type
- **DestEnumType**: Destination enum type
- **MethodName**: Generated method name (e.g., `MapToOrderStatusDto`)
- **IsSourceNullable**: Whether source is nullable
- **IsDestNullable**: Whether destination is nullable

Mapgen generates two overloads for each enum mapping:
1. Non-nullable: `TDest MethodName(TSource value)`
2. Nullable: `TDest? MethodName(TSource? value)`

### Mapping Descriptors

**BaseMappingDescriptor** - Abstract base:
- `TargetPropertyName` - Destination property

**MappingDescriptor** - Actual mapping:
- `TargetPropertyName`
- `SourceExpression` - Generated code

**IgnoredPropertyDescriptor** - Intentionally unmapped:
- `TargetPropertyName`

**DiagnosedPropertyDescriptor** - Error encountered:
- `TargetPropertyName`

**ConstructorArgumentDescriptor** - Constructor parameter mapping:
- `ParameterPosition` - Order in constructor
- `SourceExpression` - Generated code for parameter

## Design Principles

### Incremental Generation
Uses Roslyn's incremental generators for performance:
- Only regenerates when relevant code changes
- Caches intermediate results
- Minimizes compilation overhead

### Explicit Over Implicit
Mapgen favors explicitness:
- Unmapped properties are errors, not warnings
- No hidden conventions or magic
- Configuration is code, visible in source

### Type Safety
Leverages C# type system:
- All mappings type-checked at compile time
- Generic helper methods ensure type safety
- No runtime type resolution or casting

### Debuggability
Generated code is human-readable:
- No reflection or expression trees in generated code
- Can set breakpoints and step through mappings
- Go-to-definition works for all generated code

### Zero Runtime Cost
All work happens at compile time:
- No runtime initialization or scanning
- No caching or memoization needed
- Generated code is as fast as hand-written code

## Extension Points

### Future Enhancements

The architecture supports future extensions:

1. Inheritance Mapping: Support base class property mappings
2. Better experience with extra arguments in mapping methods
3. Diagnostics regarding nullable reference types
4. Diagnostics regarding required properties

## Performance Considerations

### Compilation Performance
- Incremental generation minimizes work on code changes
- Strategies applied in order, with early exit for matched properties
- Metadata models are lightweight

### Runtime Performance
- Generated code has zero overhead
- Direct property assignments (no reflection)
- Collection mappings use efficient LINQ

### Memory Efficiency
- No runtime caches or expression trees
- Mappers are transient (no state)
- Extension methods create mapper instances on-demand

## Testing Approach

### Mappers are Testable
Since mappers are regular classes:
```csharp
[Fact]
public void Should_Map_Car_To_CarDto() {
    var mapper = new CarMapper();
    var car = new Car { Id = 1, Model = "911 Turbo S" };
    
    var dto = mapper.ToDto(car);
    
    Assert.Equal(1, dto.Id);
    Assert.Equal("911 Turbo S", dto.Model);
}
```

### Integration Testing
Test the generator itself:
1. Create test mapper code
2. Run source generator
3. Compile generated code
4. Assert mappings work correctly via snapshots

## Conclusion

Mapgen's architecture prioritizes:
- **Compile-time safety**: Catch errors early
- **Performance**: Zero runtime overhead
- **Clarity**: Transparent, debuggable code
- **Flexibility**: Multiple mapping strategies
- **Maintainability**: Clean separation of concerns

The generator transforms high-level mapping declarations into efficient, type-safe C# code, giving developers the best of both worlds: convenient mapping APIs and hand-written performance.

