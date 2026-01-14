using System.Collections.Generic;
using System.Linq;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Utility methods for working with type symbols.
/// </summary>
public static class TypeSymbolExtensions
{
  extension(INamedTypeSymbol typeSymbol)
  {
    /// <summary>
    /// Gets all properties from a type, including those inherited from base classes.
    /// Properties are returned in declaration order: base class properties first (in their declaration order),
    /// then derived class properties (in their declaration order).
    /// </summary>
    public IEnumerable<IPropertySymbol> GetAllProperties()
    {
      var typeHierarchy = new Stack<INamedTypeSymbol>();
      var currentType = typeSymbol;

      // Build the type hierarchy from derived to base
      while (currentType != null)
      {
        typeHierarchy.Push(currentType);
        currentType = currentType.BaseType;
      }

      // Collect properties from base to derived, preserving declaration order within each type
      var properties = new List<IPropertySymbol>();
      foreach (var type in typeHierarchy)
      {
        var currentProperties = type.GetMembers()
          .OfType<IPropertySymbol>()
          .Where(p => !p.IsStatic) // Exclude static properties
          .OrderBy(p => p.Locations.FirstOrDefault()?.SourceSpan.Start ?? 0); // Preserve declaration order

        properties.AddRange(currentProperties);
      }

      return properties;
    }
  }
}
