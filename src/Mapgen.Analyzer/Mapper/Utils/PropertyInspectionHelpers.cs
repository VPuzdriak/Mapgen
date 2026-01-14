using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Extension methods for <see cref="IPropertySymbol"/> to inspect property characteristics
/// (system properties, readability, writability) for mapping purposes.
/// </summary>
internal static class PropertyInspectionHelpers
{
  /// <param name="property">The property to check</param>
  extension(IPropertySymbol property)
  {
    /// <summary>
    /// Determines if a property is a system/compiler-generated property that should be ignored during mapping.
    /// </summary>
    /// <returns>True if the property is a system property, false otherwise</returns>
    public bool IsSystem()
    {
      // Ignore EqualityContract property from record types
      // This is a compiler-generated property that shouldn't be mapped
      if (property.Name == "EqualityContract" && property.ContainingType.IsRecord)
      {
        return true;
      }

      return false;
    }

    /// <summary>
    /// Determines if a destination property can be set (has setter or init accessor).
    /// </summary>
    /// <returns>True if the property is settable, false otherwise</returns>
    public bool IsSettable()
    {
      // Property must have a setter (including init-only setters)
      if (property.SetMethod is null)
      {
        return false;
      }

      // Setter must be accessible (public or internal, not private or protected)
      return property.SetMethod.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
    }

    /// <summary>
    /// Determines if a source property can be read (has getter or is expression-bodied).
    /// </summary>
    /// <returns>True if the property is readable, false otherwise</returns>
    public bool IsReadable()
    {
      // Property must have a getter
      if (property.GetMethod is null)
      {
        return false;
      }

      // Getter must be accessible (public or internal, not private or protected)
      return property.GetMethod.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
    }
  }
}
