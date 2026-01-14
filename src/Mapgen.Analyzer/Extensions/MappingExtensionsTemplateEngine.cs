using System.Linq;
using System.Text;

using Microsoft.CodeAnalysis;

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
    var accessibility = GetAccessibilityModifier(_metadata.MapperClassAccessibility);
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

    foreach (var ns in _metadata.Usings)
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
    var additionalParams = method.AdditionalParameters.Count > 0
      ? ", " + string.Join(", ", method.AdditionalParameters.Select(p => $"{p.Type} {p.Name}"))
      : "";

    var additionalArgs = method.AdditionalParameters.Count > 0
      ? ", " + string.Join(", ", method.AdditionalParameters.Select(p => p.Name))
      : "";

    var builder = new StringBuilder(ExtensionMethodTemplate)
      .Replace("{{Accessibility}}", GetAccessibilityModifier(method.Accessibility))
      .Replace("{{ReturnType}}", method.ReturnType)
      .Replace("{{MethodName}}", method.MethodName)
      .Replace("{{SourceType}}", method.ExtensionParameter.Type)
      .Replace("{{SourceName}}", method.ExtensionParameter.Name)
      .Replace("{{AdditionalParameters}}", additionalParams)
      .Replace("{{AdditionalArguments}}", additionalArgs);

    return builder.ToString();
  }

  private string GetAccessibilityModifier(Accessibility accessibility) =>
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


  private const string MapperClassTemplate =
    """
    using System.Collections.Generic;

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
