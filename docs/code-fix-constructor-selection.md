# Constructor Selection Code Fix Provider

## Overview

The `AmbiguousConstructorCodeFixProvider` provides quick fixes for the MAPPER008 diagnostic, which occurs when a destination type has multiple constructors and none is explicitly specified in the mapper configuration.

## Diagnostic

**ID**: MAPPER008  
**Title**: Multiple constructors available - must specify which to use  
**Message**: Cannot generate mapping to "{TypeName}". Type has multiple constructors. Use UseConstructor() to specify which constructor parameters to use, or UseEmptyConstructor() to use the parameterless constructor.

## Code Fix Behavior

When the diagnostic is triggered, a hint icon appears with the text: **"Pick {TypeName} constructor"** where `{TypeName}` is the name of the destination type.

This hint provides nested sub-options:

1. **Use empty constructor** (if available) - Adds `UseEmptyConstructor();` to the mapper constructor
2. **Use {TypeName}(<signature>)** - For each parameterized constructor, adds a `UseConstructor(...)` call with TODO placeholders

## Example

Given a destination type with multiple constructors:

```csharp
public class CustomerDto
{
  public string Name { get; set; }
  public string Email { get; set; }
  public int Age { get; set; }

  // Parameterless constructor
  public CustomerDto() { }

  // Parameterized constructor 1
  public CustomerDto(string name, string email) { }

  // Parameterized constructor 2
  public CustomerDto(string name, string email, int age) { }
}
```

And a mapper without constructor configuration:

```csharp
[Mapper]
public partial class CustomerMapper
{
  public partial CustomerDto ToDto(Customer source);

  public CustomerMapper()
  {
    // No constructor selection - MAPPER008 diagnostic appears
  }
}
```

The code fix will offer these options:

- **"Pick CustomerDto constructor"**
  - **"Use empty constructor"**
  - **"Use CustomerDto(string name, string email)"**
  - **"Use CustomerDto(string name, string email, int age)"**

### Option 1: Use empty constructor

Selecting "Use empty constructor" will add:

```csharp
public CustomerMapper()
{
  UseEmptyConstructor();
}
```

### Option 2: Use CustomerDto(string name, string email)

Selecting this option will add:

```csharp
public CustomerMapper()
{
  UseConstructor(
    source => source.TODO_name,
    source => source.TODO_email
  );
}
```

The first `TODO_` placeholder will be automatically selected for rename, allowing the developer to quickly map the correct source members.

## Testing

To test the code fix:

1. Open `CustomerMapperEmptyConstructor.cs` in your IDE
2. Ensure the `UseEmptyConstructor();` line is commented out
3. You should see a MAPPER008 error/diagnostic on the `ToDto` method
4. Click the lightbulb/quick fix icon that appears
5. You should see "Pick CustomerDto constructor" with sub-options
6. Select one of the options to apply the fix

## Implementation Details

- **File**: `src/Mapgen.Analyzer/Mapper/CodeFixes/AmbiguousConstructorCodeFixProvider.cs`
- **Fixable Diagnostic**: MAPPER008 (AmbiguousConstructorSelection)
- **Provider Type**: ExportCodeFixProvider with nested code actions
- **Rename Annotation**: The first TODO placeholder in UseConstructor() calls is annotated for automatic rename

## Related Files

- `src/Mapgen.Analyzer/Mapper/Diagnostics/DiagnosticIds.cs` - Contains MAPPER008 ID
- `src/Mapgen.Analyzer/Mapper/Diagnostics/MapperDiagnostic.cs` - Diagnostic creation
- `src/Mapgen.Analyzer/Mapper/MapperMethodTransformer.cs` - Where diagnostic is raised
- `src/Mapgen.Analyzer/Mapper/Strategies/ConstructorMappingStrategy.cs` - Constructor analysis logic

