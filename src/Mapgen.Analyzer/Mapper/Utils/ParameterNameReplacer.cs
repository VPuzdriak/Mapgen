using System.Text.RegularExpressions;

namespace Mapgen.Analyzer.Mapper.Utils;

internal static class ParameterNameReplacer
{
  /// <summary>
  /// Replaces occurrences of a parameter name with a new name in an expression string.
  /// Uses word boundary matching to avoid replacing partial matches.
  /// </summary>
  /// <param name="expression">The expression string to process</param>
  /// <param name="oldName">The parameter name to replace</param>
  /// <param name="newName">The new parameter name</param>
  /// <returns>The expression with replaced parameter names</returns>
  public static string ReplaceParameterName(string expression, string oldName, string newName)
  {
    // Use word boundary replacement to avoid replacing partial matches
    // e.g., "src.Owner" -> "car.Owner", but not "src" inside "srcProperty"
    return Regex.Replace(
      expression,
      $@"\b{Regex.Escape(oldName)}\b",
      newName
    );
  }
}
