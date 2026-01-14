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

**Generated Public API** (in `Mapgen.Analyzer.Abstractions` namespace):
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
  2. AddIgnoreMappings
  3. AddCustomMappings
  4. AddCollectionMappings
  5. AddDirectMappings
  6. AddUnmappedPropertyDiagnostics
          ↓
MapperMethodMetadata (with mappings & diagnostics)
```

### 3. Generation Phase
```
MappingConfigurationMetadata
          ↓
MapperDiagnosticsReporter.Report()
          ↓
MapperTemplateEngine.GenerateMapperClass()
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

**Purpose**: Parse mapper dependencies from constructor

**Responsibilities**:
- Find `IncludeMappers()` calls in constructor
- Extract included mapper types from collection expressions
- Build `IncludedMapperInfo` for each dependency

**Example**:
```csharp
public CarOwnerMapper() {
    IncludeMappers([new CarMapper(), new DriverMapper()]);
}
```

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
2. Check for implicit type conversions (e.g., `int` → `long`)
3. Check for nullable/non-nullable mismatches (reports diagnostic)
4. Check for included mapper that can map between types
5. If no valid mapping found, report type mismatch diagnostic

**Example**:
```csharp
// Auto-mapped (exact match)
car.Id → carDto.Id

// Auto-mapped (implicit conversion)
car.Year (short) → carDto.Year (int)

// Auto-mapped (via included mapper)
car.Owner (CarOwner) → carDto.Owner (CarOwnerDto)
// Uses _carOwnerMapper.ToDto(car.Owner)
```

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
```

**Multiple Overloads**: The generator creates overloads for different parameter counts:
```csharp
// 1 parameter
MapMember(dto => dto.Name, car => car.Model)

// 2 parameters
MapMember(dto => dto.Name, (car, driver) => car.Model + " driven by " + driver.Name)
```

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

