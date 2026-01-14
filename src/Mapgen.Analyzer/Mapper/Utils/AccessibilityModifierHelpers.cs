using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Utils;

/// <summary>
/// Helper methods for converting Roslyn Accessibility enum to C# modifier strings (public, private, internal, etc.).
/// </summary>
internal static class AccessibilityModifierHelpers
{
  /// <summary>
  /// Converts Roslyn Accessibility enum to C# accessibility string.
  /// </summary>
  public static string GetAccessibilityModifierString(Accessibility accessibility) =>
    accessibility switch
    {
      Accessibility.Public => "public",
      Accessibility.Internal => "internal",
      Accessibility.Private => "private",
      Accessibility.Protected => "protected",
      Accessibility.ProtectedAndInternal => "protected internal",
      Accessibility.ProtectedOrInternal => "private protected",
      _ => "internal"
    };
}
