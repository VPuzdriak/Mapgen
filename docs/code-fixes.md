# Code Fixes

Mapgen provides automated code fixes to help you quickly resolve common mapping issues directly from your IDE. When Mapgen detects a problem, it not only reports a diagnostic error but also offers one-click fixes to resolve the issue.

## Table of Contents
- [Overview](#overview)
- [Unmapped Property Code Fixes](#unmapped-property-code-fixes)
- [Ambiguous Constructor Code Fixes](#ambiguous-constructor-code-fixes)
- [Using Code Fixes in Your IDE](#using-code-fixes-in-your-ide)

## Overview

Code fixes are automated refactorings that appear when Mapgen reports a diagnostic. They help you:

- ✅ **Resolve issues quickly** - One-click fixes without manual typing
- ✅ **Learn the API** - See correct configuration patterns
- ✅ **Reduce errors** - Automated code generation prevents typos
- ✅ **Save time** - Focus on business logic, not boilerplate

### Supported Diagnostics with Code Fixes

| Diagnostic | Description | Code Fix Provider |
|------------|-------------|-------------------|
| MAPPER001 | Unmapped property | [Unmapped Property Code Fixes](#unmapped-property-code-fixes) |
| MAPPER008 | Ambiguous constructor | [Ambiguous Constructor Code Fixes](#ambiguous-constructor-code-fixes) |

## Unmapped Property Code Fixes

When a destination property cannot be automatically mapped from the source, Mapgen reports a **MAPPER001** diagnostic. The code fix provider offers two options to resolve the issue.

### When It Triggers

The unmapped property code fix appears when:
- A destination property has no matching source property (by name)
- Types are incompatible and no implicit conversion exists
- You need custom transformation logic

### Example Scenario

```csharp
public class Person
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public int Age { get; set; }
}

public class PersonDto
{
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string FullName { get; set; }  // ❌ MAPPER001: No matching source property
    public int Age { get; set; }
}

[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);
    // ERROR: Property "FullName" cannot be mapped
}
```

### Available Code Fixes

When you see the MAPPER001 error, Mapgen offers a code fix menu: **"Fix FullName member"** with two nested options:

#### Option 1: Add MapMember

Creates a custom mapping configuration for the unmapped property.

**Before:**
```csharp
[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);
    // ERROR: Property "FullName" cannot be mapped
}
```

**After applying "Add MapMember" fix:**
```csharp
[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);

    public PersonMapper()
    {
        MapMember(dest => dest.FullName, src => src.TODO);
    }
}
```

**What happens:**
1. Creates a parameterless constructor if one doesn't exist
2. Adds a `MapMember()` configuration for the unmapped property
3. Uses `TODO` as a placeholder that triggers IntelliSense
4. Automatically places cursor at `TODO` for easy editing

**Complete the fix:**
Replace `TODO` with your mapping logic:
```csharp
public PersonMapper()
{
    MapMember(dest => dest.FullName, src => $"{src.FirstName} {src.LastName}");
}
```

#### Option 2: Add IgnoreMember

Explicitly marks the property as ignored, preventing Mapgen from attempting to map it.

**After applying "Add IgnoreMember" fix:**
```csharp
[Mapper]
public partial class PersonMapper
{
    public partial PersonDto ToDto(Person source);

    public PersonMapper()
    {
        IgnoreMember(dest => dest.FullName);
    }
}
```

### Complex Scenarios

#### Multiple Unmapped Properties

If multiple properties are unmapped, you can apply fixes one at a time:

```csharp
public class OrderDto
{
    public int Id { get; set; }
    public string Status { get; set; }       // ❌ MAPPER001
    public decimal TotalPrice { get; set; }  // ❌ MAPPER001
}
```

Apply fixes sequentially:

```csharp
public OrderMapper()
{
    MapMember(dest => dest.Status, src => src.OrderStatus.ToString());
    MapMember(dest => dest.TotalPrice, src => src.Items.Sum(i => i.Price));
}
```

#### Unmapped Properties with Existing Constructor

If your mapper already has a constructor with configuration, the code fix appends the new statement:

**Before:**
```csharp
public PersonMapper()
{
    MapMember(dest => dest.Email, src => src.EmailAddress);
}
```

**After fix:**
```csharp
public PersonMapper()
{
    MapMember(dest => dest.Email, src => src.EmailAddress);
    MapMember(dest => dest.FullName, src => src.TODO);  // ✅ Added by code fix
}
```

### Code Fix Behavior

**Constructor Creation:**
- If no constructor exists, code fix creates a parameterless one
- Constructor is inserted after the mapping method

**Statement Insertion:**
- New configuration added to end of constructor body
- TODO placeholder positioned for IntelliSense rename

## Ambiguous Constructor Code Fixes

When a destination type has multiple constructors, Mapgen reports a **MAPPER008** diagnostic. The code fix provider helps you choose which constructor to use.

### When It Triggers

The ambiguous constructor code fix appears when:
- Destination type has 2 or more constructors (parameterless and/or parameterized)
- No `UseConstructor()` or `UseEmptyConstructor()` is configured
- Mapgen cannot automatically determine which constructor to use

### Example Scenario

```csharp
public class ProductDto
{
    public string Name { get; }
    public decimal Price { get; }

    // Multiple constructors - ambiguous!
    public ProductDto() { }
    public ProductDto(string name, decimal price)
    {
        Name = name;
        Price = price;
    }
}

[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);
    // ERROR MAPPER008: Ambiguous constructor selection
}
```

### Available Code Fixes

When you see the MAPPER008 error, Mapgen offers a code fix menu: **"Pick ProductDto constructor"** with options for each available constructor:

#### Option 1: Use Empty Constructor

Configures the mapper to use the parameterless constructor.

**After applying "Use empty constructor" fix:**
```csharp
[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);

    public ProductMapper()
    {
        UseEmptyConstructor();
    }
}
```

**Generated mapping code:**
```csharp
return new ProductDto
{
    Name = source.Name,
    Price = source.Price
};
```

#### Option 2: Use Parameterized Constructor

Generates `UseConstructor()` configuration with TODO placeholders for each parameter.

**After applying "Use ProductDto(string name, decimal price)" fix:**
```csharp
[Mapper]
public partial class ProductMapper
{
    public partial ProductDto ToDto(Product source);

    public ProductMapper()
    {
        UseConstructor(
          source => source.TODO,
          source => source.TODO
        );
    }
}
```

**Complete the fix:**
Replace each `TODO` with the appropriate source property:

```csharp
public ProductMapper()
{
    UseConstructor(
        source => source.Name,
        source => source.Price
    );
}
```

**Generated mapping code:**
```csharp
return new ProductDto(
    source.Name,
    source.Price
);
```

### Multiple Constructors Example

When there are multiple parameterized constructors, each appears as a separate option:

```csharp
public class OrderDto
{
    public int Id { get; }
    public string Status { get; }
    public decimal Total { get; }

    public OrderDto() { }
    public OrderDto(int id) { /* ... */ }
    public OrderDto(int id, string status) { /* ... */ }
    public OrderDto(int id, string status, decimal total) { /* ... */ }
}
```

**Code fix menu shows:**
- Use empty constructor
- Use OrderDto(int id)
- Use OrderDto(int id, string status)
- Use OrderDto(int id, string status, decimal total)

### Constructor Signatures in Code Fix

The code fix displays constructor signatures to help you choose:

```
Pick OrderDto constructor
  └─ Use empty constructor
  └─ Use OrderDto(int id)
  └─ Use OrderDto(int id, string status)
  └─ Use OrderDto(int id, string status, decimal total)
```

Each option shows:
- Parameter count
- Parameter types and names
- Clear distinction between constructors

### Code Fix Behavior

**Constructor Creation:**
- Creates parameterless mapper constructor if needed
- Inserts constructor after first mapping method
- Maintains proper formatting and indentation

**Single Parameter:**
```csharp
UseConstructor(source => source.TODO);
```

**Multiple Parameters:**
```csharp
UseConstructor(
  source => source.TODO,
  source => source.TODO
);
```

### Complex Scenarios

#### Existing Constructor Configuration

If mapper already has a constructor with other configuration:

**Before:**
```csharp
public OrderMapper()
{
    MapMember(dest => dest.DisplayName, src => src.CustomerName);
}
```

**After fix:**
```csharp
public OrderMapper()
{
    MapMember(dest => dest.DisplayName, src => src.CustomerName);
    UseConstructor(
        source => source.TODO,
        source => source.TODO
    );
}
```

#### Multi-Line Format

For constructors with 2+ parameters, code fix uses multi-line format:

```csharp
UseConstructor(
  source => source.TODO,
  source => source.TODO,
  source => source.TODO
);
```

#### Records with Primary Constructors

For record types, constructor parameters match positional parameters:

```csharp
public record ProductDto(string Name, decimal Price);

// Code fix generates:
UseConstructor(
  source => source.TODO,
  source => source.TODO
);
```

## Using Code Fixes in Your IDE

### Visual Studio

1. **Locate the error:** Look for red squiggle under the diagnostic
2. **Open quick actions:** 
   - Click the lightbulb 💡 icon
   - Or press `Ctrl+.` (Windows) / `Cmd+.` (Mac)
3. **Select fix:** Choose from the nested menu
4. **Apply:** Click to apply the fix
5. **Edit placeholders:** Replace TODO with actual values

### Rider

1. **Locate the error:** Look for red squiggle or error highlighting
2. **Open quick actions:**
   - Click the lightbulb/wrench icon
   - Or press `Alt+Enter`
3. **Select fix:** Navigate through the nested menu
4. **Apply:** Press `Enter` to apply
5. **Rename TODO:** Press `F2` or automatic rename triggers

### VS Code (with C# extension)

1. **Locate the error:** Look for red squiggle
2. **Open quick actions:**
   - Click the lightbulb 💡 icon
   - Or press `Ctrl+.` (Windows/Linux) / `Cmd+.` (Mac)
3. **Select fix:** Choose from available fixes
4. **Apply:** Click to apply
5. **Edit placeholders:** Replace TODO values

### Keyboard Shortcuts

| IDE | Shortcut | Action |
|-----|----------|--------|
| Visual Studio | `Ctrl+.` | Show quick actions |
| Rider | `Alt+Enter` | Show context actions |
| VS Code | `Ctrl+.` or `Cmd+.` | Show quick fixes |

### Pro Tips

**Batch Fixes Not Supported:**
- Code fixes apply one diagnostic at a time
- This is intentional - each fix requires context-specific decisions
- Apply fixes sequentially for multiple diagnostics

**Undo Support:**
- All code fixes support undo (`Ctrl+Z` / `Cmd+Z`)
- Safe to experiment with different options

## Troubleshooting

### Code Fix Not Appearing

**Issue:** Lightbulb/quick action doesn't appear

**Solutions:**
1. **Verify diagnostic exists:** Ensure you see MAPPER001 or MAPPER008 error
2. **Rebuild project:** Code fixes depend on Roslyn analyzer
3. **Cursor position:** Place cursor on the error squiggle
4. **Check IDE version:** Ensure your IDE supports Roslyn code fixes

### Constructor Already Exists with Parameters

**Issue:** Code fix tries to create constructor but one exists with parameters

**Solution:**
Mapgen doesn't modify constructors with parameters. Remove parameters first:

```csharp
// ❌ This prevents code fix
public PersonMapper(ILogger logger)
{
    MapMember(dest => dest.Email, src => src.EmailAddress);
}

// ✅ Remove parameters, then apply code fix
public PersonMapper()
{
    MapMember(dest => dest.Email, src => src.EmailAddress);
    // Code fix can now add configuration here
}
```
## See Also

- [Core Features](core-features.md) - Automatic mapping and configuration methods
- [Advanced Usage](advanced-usage.md) - Constructor mapping and diagnostics
- [Getting Started](getting-started.md) - Mapgen basics
- [Best Practices](migration/best-practices.md) - Mapping patterns and conventions

## Summary

Mapgen's code fix providers accelerate development by:

- ✅ **Reducing boilerplate** - Automated code generation
- ✅ **Preventing errors** - Correct syntax guaranteed
- ✅ **Teaching the API** - Learn patterns through examples
- ✅ **Saving time** - Focus on business logic

Use code fixes as interactive documentation that generates correct code while you learn Mapgen's API.

