using System.Linq;
using System.Text;

using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper;

public sealed class MapperTemplateEngine
{
  private readonly MappingConfigurationMetadata _configMetadata;

  public MapperTemplateEngine(MappingConfigurationMetadata configMetadata)
  {
    _configMetadata = configMetadata;
  }

  public string MapperConfigFileName => $"{_configMetadata.MapperName}.g.cs";

  public string GenerateMapperClass()
  {
    var usings = GenerateUsings();
    var classAccessibility = GetAccessibilityString(_configMetadata.MapperAccessibility);
    var mapperFields = GenerateMapperFields();
    var constructor = GenerateConstructor();
    var methods = GenerateMethods();
    var helperMethods = GenerateHelperMethods();
    var builder = new StringBuilder(MapperClassTemplate)
      .Replace("{{Usings}}", usings)
      .Replace("{{Namespace}}", _configMetadata.MapperNamespace)
      .Replace("{{ClassAccessibility}}", classAccessibility)
      .Replace("{{ClassName}}", _configMetadata.MapperName)
      .Replace("{{MapperFields}}", mapperFields)
      .Replace("{{Constructor}}", constructor)
      .Replace("{{Methods}}", methods)
      .Replace("{{HelperMethods}}", helperMethods);

    return builder.ToString();
  }

  private string GenerateUsings()
  {
    var builder = new StringBuilder();

    foreach (var ns in _configMetadata.Usings)
    {
      builder.AppendLine($"using {ns};");
    }

    return builder.ToString();
  }

  private string GetAccessibilityString(Accessibility accessibility) =>
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

  private string GenerateMapperFields()
  {
    if (_configMetadata.Method is null || !_configMetadata.Method.IncludedMappers.Any())
    {
      return string.Empty;
    }

    var builder = new StringBuilder();
    foreach (var mapper in _configMetadata.Method.IncludedMappers)
    {
      builder.AppendLine($"    private readonly {mapper.MapperType.Name} {mapper.FieldName} = new {mapper.MapperType.Name}();");
    }

    return builder.ToString();
  }

  private string GenerateConstructor()
  {
    // Don't generate constructor - user already has one where they call IncludeMappers
    return string.Empty;
  }


  private string GenerateMethods()
  {
    if (_configMetadata.Method is null)
    {
      return string.Empty;
    }

    var methodCode = GenerateMethod(_configMetadata.Method);
    return methodCode;
  }

  private string GenerateMethod(MapperMethodMetadata methodMetadata)
  {
    var accessibility = GetAccessibilityString(methodMetadata.MethodAccessibility);
    var parameters = GenerateMethodParameters(methodMetadata);
    var mappings = GenerateMethodMappings(methodMetadata);

    var builder = new StringBuilder(MapperClassMethodTemplate)
      .Replace("{{MethodAccessibility}}", accessibility)
      .Replace("{{ReturnType}}", $"{methodMetadata.ReturnTypeName}")
      .Replace("{{MethodName}}", methodMetadata.MethodName)
      .Replace("{{Parameters}}", parameters)
      .Replace("{{Mappings}}", mappings);

    return builder.ToString();
  }

  private string GenerateMethodParameters(MapperMethodMetadata methodMetadata)
    => string.Join(", ", methodMetadata.Parameters.Select(p => $"{p.Type} {p.Name}"));

  private string GenerateMethodMappings(MapperMethodMetadata methodMetadata)
  {
    var builder = new StringBuilder();
    foreach (var mapping in methodMetadata.Mappings.OfType<MappingDescriptor>())
    {
      var mappingCode = GenerateMapping(mapping);
      builder.AppendLine(mappingCode);
    }

    return builder.ToString().TrimEnd();
  }

  private string GenerateMapping(MappingDescriptor mapping)
  {
    return $"        {mapping.TargetPropertyName} = {mapping.SourceExpression},";
  }

  private string GenerateHelperMethods()
  {
    if (_configMetadata.Method is null)
    {
      return string.Empty;
    }

    var builder = new StringBuilder();

    var includeMappersMethod = GenerateIncludeMappersMethod();
    builder.AppendLine(includeMappersMethod + "\n");

    var mapMemberMethod = GenerateMapMemberMethod(_configMetadata.Method);
    builder.AppendLine(mapMemberMethod + "\n");

    var mapCollectionMethod = GenerateMapCollectionMethod(_configMetadata.Method);
    builder.AppendLine(mapCollectionMethod + "\n");

    var ignoreMemberMethod = GenerateIgnoreMemberMethod(_configMetadata.Method);
    builder.AppendLine(ignoreMemberMethod);

    return builder.ToString();
  }

  private string GenerateIncludeMappersMethod()
  {
    return """
               private void IncludeMappers(object[] mappers) {
                 // Mapgen will use this method to include other mappers.
               }
           """;
  }

  private string GenerateMapMemberMethod(MapperMethodMetadata methodMetadata)
  {
    var destinationType = methodMetadata.ReturnTypeName;
    var builder = new StringBuilder();

    // Generate overloads for 1 parameter up to the total number of parameters
    // For example, if the method has 3 parameters (car, driver, garage), generate:
    // 1. MapMember with 1 parameter: (car)
    // 2. MapMember with 2 parameters: (car, driver)
    // 3. MapMember with 3 parameters: (car, driver, garage)

    for (var paramCount = 1; paramCount <= methodMetadata.Parameters.Count; paramCount++)
    {
      if (builder.Length > 0)
      {
        builder.AppendLine().AppendLine();
      }

      var parameters = methodMetadata.Parameters.Take(paramCount).ToList();
      var sourceParamTypes = string.Join(", ", parameters.Select(p => p.Type));
      var funcSignature = $"Func<{sourceParamTypes}, TDestinationMember>";

      var overload = $$"""
                           private void {{Constants.MapMemberMethodName}}<TDestinationMember>(
                             Expression<Func<{{destinationType}}, TDestinationMember>> destinationMember,
                             {{funcSignature}} sourceFunc) {
                             // Mapgen will use this method as mapping configuration.
                           }
                       """;

      builder.Append(overload);
    }

    return builder.ToString();
  }

  private string GenerateMapCollectionMethod(MapperMethodMetadata methodMetadata)
  {
    var builder = new StringBuilder();

    // Generate overloads with single property expression (destination only)
    var singleExpressionOverloads = GenerateMapCollectionWithSingleExpression(methodMetadata);
    builder.Append(singleExpressionOverloads);

    // Generate overloads with two property expressions (destination + source)
    var twoExpressionsOverloads = GenerateMapCollectionWithTwoExpressions(methodMetadata);
    if (!string.IsNullOrEmpty(twoExpressionsOverloads))
    {
      builder.AppendLine().AppendLine();
      builder.Append(twoExpressionsOverloads);
    }

    return builder.ToString();
  }

  private string GenerateMapCollectionWithSingleExpression(MapperMethodMetadata methodMetadata)
  {
    // Generates: MapCollection(dto => dto.Collection, item => item.ToDto())
    // Single property expression: destination collection
    var destinationType = methodMetadata.ReturnTypeName;
    var builder = new StringBuilder();

    // Base overload: item only
    var baseOverload = $$"""
                             private void MapCollection<TDestinationItem, TSourceItem>(
                               Expression<Func<{{destinationType}}, object>> destinationCollection,
                               Func<TSourceItem, TDestinationItem> itemTransform) {
                               // Mapgen will use this method as mapping configuration.
                             }
                         """;
    builder.Append(baseOverload);

    // Parameterized overloads: item + mapper parameters
    for (var paramCount = 1; paramCount <= methodMetadata.Parameters.Count; paramCount++)
    {
      builder.AppendLine().AppendLine();

      var parameters = methodMetadata.Parameters.Take(paramCount).ToList();
      var paramTypes = new[] { "TSourceItem" }.Concat(parameters.Select(p => p.Type));
      var funcSignature = $"Func<{string.Join(", ", paramTypes)}, TDestinationItem>";

      var overload = $$"""
                           private void MapCollection<TDestinationItem, TSourceItem>(
                             Expression<Func<{{destinationType}}, object>> destinationCollection,
                             {{funcSignature}} itemTransform) {
                             // Mapgen will use this method as mapping configuration.
                           }
                       """;

      builder.Append(overload);
    }

    return builder.ToString();
  }

  private string GenerateMapCollectionWithTwoExpressions(MapperMethodMetadata methodMetadata)
  {
    // Generates: MapCollection(dto => dto.DtoCollection, source => source.SourceCollection, item => item.ToDto())
    // Two property expressions: destination collection + source collection
    var destinationType = methodMetadata.ReturnTypeName;
    var builder = new StringBuilder();

    // Only generate these if there are mapper parameters
    if (methodMetadata.Parameters.Count == 0)
    {
      return string.Empty;
    }

    var firstSourceParam = methodMetadata.Parameters.First();

    // Base overload: item only
    var baseOverload = $$"""
                             private void MapCollection<TDestinationItem, TSourceItem>(
                               Expression<Func<{{destinationType}}, object>> destinationCollection,
                               Expression<Func<{{firstSourceParam.Type}}, IEnumerable<TSourceItem>>> sourceCollection,
                               Func<TSourceItem, TDestinationItem> itemTransform) {
                               // Mapgen will use this method as mapping configuration.
                             }
                         """;
    builder.Append(baseOverload);

    // Parameterized overloads: item + mapper parameters
    for (var paramCount = 1; paramCount <= methodMetadata.Parameters.Count; paramCount++)
    {
      builder.AppendLine().AppendLine();

      var parameters = methodMetadata.Parameters.Take(paramCount).ToList();
      var paramTypes = new[] { "TSourceItem" }.Concat(parameters.Select(p => p.Type));
      var funcSignature = $"Func<{string.Join(", ", paramTypes)}, TDestinationItem>";

      var overload = $$"""
                           private void MapCollection<TDestinationItem, TSourceItem>(
                             Expression<Func<{{destinationType}}, object>> destinationCollection,
                             Expression<Func<{{firstSourceParam.Type}}, IEnumerable<TSourceItem>>> sourceCollection,
                             {{funcSignature}} itemTransform) {
                             // Mapgen will use this method as mapping configuration.
                           }
                       """;

      builder.Append(overload);
    }

    return builder.ToString();
  }

  private string GenerateIgnoreMemberMethod(MapperMethodMetadata methodMetadata)
  {
    var destinationType = methodMetadata.ReturnTypeName;
    return $$"""
                 private void {{Constants.IgnoreMemberMethodName}}<TDestinationMember>(
                   Expression<Func<{{destinationType}}, TDestinationMember>> destinationMember) {
                   // Mapgen will use this method as mapping configuration.
                 }
             """;
  }

  private const string MapperClassTemplate =
    """
    using System;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using System.Linq.Expressions;

    {{Usings}}

    namespace {{Namespace}} {
      {{ClassAccessibility}} partial class {{ClassName}} {
    {{MapperFields}}
    {{Constructor}}
    {{Methods}}

    {{HelperMethods}}
      }
    }
    """;

  private const string MapperClassMethodTemplate =
    """
        {{MethodAccessibility}} partial {{ReturnType}} {{MethodName}}({{Parameters}}) {
          return new {{ReturnType}} {
    {{Mappings}}
          };
        }
    """;
}
