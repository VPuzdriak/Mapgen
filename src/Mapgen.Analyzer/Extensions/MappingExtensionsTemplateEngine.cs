using System.Linq;
using System.Text;

using Mapgen.Analyzer.Mapper.Utils;

namespace Mapgen.Analyzer.Extensions;

public sealed class MappingExtensionsTemplateEngine
{
  private readonly MapperExtensionsMetadata _metadata;

  public MappingExtensionsTemplateEngine(MapperExtensionsMetadata metadata)
  {
    _metadata = metadata;
  }

  public string GenerateExtensionsClass()
  {
    var usings = GenerateUsings();
    var accessibility = AccessibilityModifierHelpers.GetAccessibilityModifierString(_metadata.MapperClassAccessibility);
    var className = _metadata.MapperClassName + "Extensions";
    var methods = GenerateMethods();

    var builder = new StringBuilder(MapperClassTemplate)
      .Replace("{{Usings}}", usings)
      .Replace("{{Namespace}}", _metadata.MapperNamespace)
      .Replace("{{ClassAccessibility}}", accessibility)
      .Replace("{{ClassName}}", className)
      .Replace("{{MapperClassName}}", _metadata.MapperClassName)
      .Replace("{{Methods}}", methods);

    return builder.ToString();
  }

  private string GenerateUsings()
  {
    var builder = new StringBuilder();

    // Ensure required system namespaces are always included
    var requiredUsings = new[] { "System.Collections.Generic" };

    var allUsings = requiredUsings
      .Concat(_metadata.Usings)
      .Distinct();

    foreach (var ns in allUsings)
    {
      builder.AppendLine($"using {ns};");
    }

    return builder.ToString();
  }

  private string GenerateMethods()
  {
    var builder = new StringBuilder();

    foreach (var method in _metadata.ExtensionMethods)
    {
      builder.AppendLine(GenerateExtensionMethod(method));
    }

    return builder.ToString();
  }

  private string GenerateExtensionMethod(ExtensionMethodInfo method)
  {
    var returnType = _metadata.TypeAliasResolver.GetTypeDisplayString(method.ReturnTypeSymbol);
    var sourceType = _metadata.TypeAliasResolver.GetTypeDisplayString(method.ExtensionParameter.TypeSymbol);

    var additionalParams = method.AdditionalParameters.Count > 0
      ? ", " + string.Join(", ", method.AdditionalParameters.Select(p =>
        {
          var paramType = _metadata.TypeAliasResolver.GetTypeDisplayString(p.TypeSymbol);
          return $"{paramType} {p.Name}";
        }))
      : "";

    var additionalArgs = method.AdditionalParameters.Count > 0
      ? ", " + string.Join(", ", method.AdditionalParameters.Select(p => p.Name))
      : "";

    var builder = new StringBuilder(ExtensionMethodTemplate)
      .Replace("{{Accessibility}}", AccessibilityModifierHelpers.GetAccessibilityModifierString(method.Accessibility))
      .Replace("{{ReturnType}}", returnType)
      .Replace("{{MethodName}}", method.MethodName)
      .Replace("{{SourceType}}", sourceType)
      .Replace("{{SourceName}}", method.ExtensionParameter.Name)
      .Replace("{{AdditionalParameters}}", additionalParams)
      .Replace("{{AdditionalArguments}}", additionalArgs);

    return builder.ToString();
  }



  private const string MapperClassTemplate =
    """

    {{Usings}}

    namespace {{Namespace}} {
      {{ClassAccessibility}} static class {{ClassName}} {
        private static readonly {{MapperClassName}} _mapper = new {{MapperClassName}}();

    {{Methods}}
      }
    }
    """;

  private const string ExtensionMethodTemplate =
    """
        {{Accessibility}} static {{ReturnType}} {{MethodName}}(this {{SourceType}} {{SourceName}}{{AdditionalParameters}}) {
          return _mapper.{{MethodName}}({{SourceName}}{{AdditionalArguments}});
        }
    """;
}
