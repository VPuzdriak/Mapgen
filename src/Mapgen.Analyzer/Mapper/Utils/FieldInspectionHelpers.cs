using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Extension methods for <see cref="IFieldSymbol"/> to inspect field characteristics
/// (readability, writability) for mapping purposes.
/// </summary>
internal static class FieldInspectionHelpers
{
  /// <param name="field">The field to check</param>
  extension(IFieldSymbol field)
  {
    /// <summary>
    /// Determines if a field can be read (is accessible).
    /// </summary>
    /// <returns>True if the field is readable, false otherwise</returns>
    public bool IsReadable()
    {
      // Field must be accessible (public or internal, not private or protected)
      return field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
    }

    /// <summary>
    /// Determines if a field can be written to (is not readonly and is accessible).
    /// </summary>
    /// <returns>True if the field is writable, false otherwise</returns>
    public bool IsWritable()
    {
      // Field must not be readonly
      if (field.IsReadOnly)
      {
        return false;
      }

      // Field must be accessible (public or internal, not private or protected)
      return field.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal;
    }
  }
}
