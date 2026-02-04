using System.Collections.Generic;
using System.Linq;
using System.Text;

using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using SymbolDisplayFormat = Microsoft.CodeAnalysis.SymbolDisplayFormat;

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
    var nullableDirective = _configMetadata.NullableEnabled ? "#nullable enable" : "#nullable disable";
    var classAccessibility = AccessibilityModifierHelpers.GetAccessibilityModifierString(_configMetadata.MapperAccessibility);
    var mapperFields = GenerateMapperFields();
    var methods = GenerateMethods();
    var helperMethods = GenerateHelperMethods();
    var builder = new StringBuilder(MapperClassTemplate)
      .Replace("{{NullableDirective}}", nullableDirective)
      .Replace("{{Usings}}", usings)
      .Replace("{{Namespace}}", _configMetadata.MapperNamespace)
      .Replace("{{ClassAccessibility}}", classAccessibility)
      .Replace("{{ClassName}}", _configMetadata.MapperName)
      .Replace("{{MapperFields}}", mapperFields)
      .Replace("{{Methods}}", methods)
      .Replace("{{HelperMethods}}", helperMethods);

    return builder.ToString();
  }

  private string GenerateUsings()
  {
    var builder = new StringBuilder();

    // Build list of required system namespaces
    var requiredUsings = new List<string> { "System", "System.Collections.Generic", "System.Linq.Expressions" };

    var allUsings = requiredUsings
      .Concat(_configMetadata.Usings)
      .Concat(_configMetadata.Method?.RequiredUsings ?? [])
      .Distinct();

    foreach (var ns in allUsings)
    {
      builder.AppendLine($"using {ns};");
    }

    return builder.ToString();
  }

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
    var accessibility = AccessibilityModifierHelpers.GetAccessibilityModifierString(methodMetadata.MethodAccessibility);
    var parameters = GenerateMethodParameters(methodMetadata);
    var returnTypeName = methodMetadata.ReturnTypeSyntax;

    // Check if we need to use constructor with parameters
    if (methodMetadata.ConstructorInfo != null)
    {
      var constructorCall = GenerateConstructorCall(methodMetadata);

      var builder = new StringBuilder(MapperClassMethodWithConstructorTemplate)
        .Replace("{{MethodAccessibility}}", accessibility)
        .Replace("{{ReturnType}}", returnTypeName)
        .Replace("{{MethodName}}", methodMetadata.MethodName)
        .Replace("{{Parameters}}", parameters)
        .Replace("{{ConstructorCall}}", constructorCall);

      return builder.ToString();
    }
    else
    {
      // Use standard object initializer syntax
      var mappings = GenerateMethodMappings(methodMetadata);

      var builder = new StringBuilder(MapperClassMethodTemplate)
        .Replace("{{MethodAccessibility}}", accessibility)
        .Replace("{{ReturnType}}", returnTypeName)
        .Replace("{{MethodName}}", methodMetadata.MethodName)
        .Replace("{{Parameters}}", parameters)
        .Replace("{{Mappings}}", mappings);

      return builder.ToString();
    }
  }

  private string GenerateMethodParameters(MapperMethodMetadata methodMetadata)
  {
    // Use alias-aware type names for parameters, preferring original syntax
    var parameters = methodMetadata.Parameters.Select(p =>
    {
      var typeName = p.TypeSyntax;
      return $"{typeName} {p.Name}";
    });
    return string.Join(", ", parameters);
  }

  private string GenerateMethodMappings(MapperMethodMetadata methodMetadata)
  {
    // Get all members (properties and fields) in declaration order to use as sorting reference
    var memberOrder = methodMetadata.ReturnType.GetAllMembers()
      .Select((member, index) => new { MemberName = member.Name, Order = index })
      .ToDictionary(x => x.MemberName, x => x.Order);

    // Sort mappings by the declaration order of destination members
    var sortedMappings = methodMetadata.Mappings
      .OfType<MappingDescriptor>()
      .OrderBy(m => memberOrder.TryGetValue(m.TargetMemberName, out var order) ? order : int.MaxValue);

    var builder = new StringBuilder();
    foreach (var mapping in sortedMappings)
    {
      var mappingCode = GenerateMapping(mapping);
      builder.AppendLine(mappingCode);
    }

    return builder.ToString().TrimEnd();
  }

  private string GenerateMapping(MappingDescriptor mapping)
  {
    return $"        {mapping.TargetMemberName} = {mapping.SourceExpression},";
  }

  private string GenerateConstructorCall(MapperMethodMetadata methodMetadata)
  {
    if (methodMetadata.ConstructorInfo is null)
    {
      return string.Empty;
    }

    var returnTypeName = methodMetadata.ReturnTypeSyntax;
    var builder = new StringBuilder();

    // Get constructor parameter names (from the ConstructorInfo)
    var constructorParamNames = new System.Collections.Generic.HashSet<string>(
      methodMetadata.ConstructorInfo.Parameters.Select(p => p.Name),
      System.StringComparer.OrdinalIgnoreCase);

    // Find mappings that correspond to constructor arguments
    var constructorMappings = methodMetadata.Mappings
      .OfType<ConstructorArgumentDescriptor>()
      .OrderBy(m => m.ParameterPosition)
      .ToList();

    // Find mappings for properties that need object initializer (not constructor arguments)
    var initializerMappings = methodMetadata.Mappings
      .OfType<MappingDescriptor>()
      .Where(m => !constructorParamNames.Contains(m.TargetMemberName))
      .OrderBy(m =>
      {
        var memberOrder = methodMetadata.ReturnType.GetAllMembers()
          .Select((member, index) => new { MemberName = member.Name, Order = index })
          .ToDictionary(x => x.MemberName, x => x.Order);
        return memberOrder.TryGetValue(m.TargetMemberName, out var order) ? order : int.MaxValue;
      })
      .ToList();

    // Generate constructor call
    builder.Append($"return new {returnTypeName}(");

    if (constructorMappings.Any())
    {
      builder.AppendLine();
      for (var i = 0; i < constructorMappings.Count; i++)
      {
        var mapping = constructorMappings[i];
        var comma = i < constructorMappings.Count - 1 ? "," : "";
        builder.AppendLine($"        {mapping.SourceExpression}{comma}");
      }

      builder.Append("      )");
    }
    else
    {
      builder.Append(")");
    }

    // Add object initializer if there are properties not set by constructor
    if (initializerMappings.Any())
    {
      builder.AppendLine(" {");
      foreach (var mapping in initializerMappings)
      {
        builder.AppendLine($"        {mapping.TargetMemberName} = {mapping.SourceExpression},");
      }

      builder.Append("      }");
    }

    builder.Append(";");
    return builder.ToString();
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

    var useConstructorMethods = GenerateUseConstructorMethods(_configMetadata.Method);
    builder.AppendLine(useConstructorMethods + "\n");

    var useEmptyConstructorMethod = GenerateUseEmptyConstructorMethod();
    builder.AppendLine(useEmptyConstructorMethod + "\n");

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

  private string GenerateUseConstructorMethods(MapperMethodMetadata methodMetadata)
  {
    var builder = new StringBuilder();
    var destinationType = methodMetadata.ReturnType;

    // Get all public constructors with parameters
    var constructorsWithParams = destinationType.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Microsoft.CodeAnalysis.Accessibility.Public && c.Parameters.Length > 0)
      .OrderBy(c => c.Parameters.Length)
      .ToList();

    // Generate an overload for each constructor
    foreach (var constructor in constructorsWithParams)
    {
      if (builder.Length > 0)
      {
        builder.AppendLine().AppendLine();
      }

      var sourceParamTypes = string.Join(", ", methodMetadata.Parameters.Select(p => p.TypeSyntax));

      var parameters = new List<string>();
      foreach (var ctorParam in constructor.Parameters)
      {
        var paramType = ctorParam.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
        var paramName = ctorParam.Name;
        parameters.Add($"Expression<Func<{sourceParamTypes}, {paramType}>> {paramName}");
      }

      var parametersStr = string.Join(",\n      ", parameters.ToArray());

      var method = $$"""
                         private void UseConstructor(
                           {{parametersStr}})
                         {
                           // Mapgen will use this method for constructor configuration.
                         }
                     """;

      builder.Append(method);
    }

    // If no constructors with parameters, generate a fallback (shouldn't happen in practice)
    if (constructorsWithParams.Count == 0)
    {
      builder.Append("""
                         private void UseConstructor()
                         {
                           // No parameterized constructors available.
                         }
                     """);
    }

    return builder.ToString();
  }

  private string GenerateUseEmptyConstructorMethod()
  {
    return """
               private void UseEmptyConstructor()
               {
                 // Mapgen will use this method to explicitly use parameterless constructor.
               }
           """;
  }

  private string GenerateMapMemberMethod(MapperMethodMetadata methodMetadata)
  {
    var destinationType = methodMetadata.ReturnTypeSyntax;
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
      var sourceParamTypes = string.Join(", ", parameters.Select(p => p.TypeSyntax));
      var funcSignature = $"Func<{sourceParamTypes}, TDestinationMember>";

      var overload = $$"""
                           private void {{MappingConfigurationMethods.MapMemberMethodName}}<TDestinationMember>(
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
    var destinationType = methodMetadata.ReturnTypeSyntax;
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
      var paramTypes = new[] { "TSourceItem" }.Concat(parameters.Select(p => p.TypeSyntax));
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
    var destinationType = methodMetadata.ReturnTypeSyntax;
    var builder = new StringBuilder();

    // Only generate these if there are mapper parameters
    if (methodMetadata.Parameters.Count == 0)
    {
      return string.Empty;
    }

    var firstSourceParam = methodMetadata.Parameters.First();
    var firstSourceParamType = firstSourceParam.TypeSyntax;

    // Base overload: item only
    var baseOverload = $$"""
                             private void MapCollection<TDestinationItem, TSourceItem>(
                               Expression<Func<{{destinationType}}, object>> destinationCollection,
                               Expression<Func<{{firstSourceParamType}}, IEnumerable<TSourceItem>>> sourceCollection,
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
      var paramTypes = new[] { "TSourceItem" }.Concat(parameters.Select(p => p.TypeSyntax));
      var funcSignature = $"Func<{string.Join(", ", paramTypes)}, TDestinationItem>";

      var overload = $$"""
                           private void MapCollection<TDestinationItem, TSourceItem>(
                             Expression<Func<{{destinationType}}, object>> destinationCollection,
                             Expression<Func<{{firstSourceParamType}}, IEnumerable<TSourceItem>>> sourceCollection,
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
    var destinationType = methodMetadata.ReturnTypeSyntax;
    return $$"""
                 private void {{MappingConfigurationMethods.IgnoreMemberMethodName}}<TDestinationMember>(
                   Expression<Func<{{destinationType}}, TDestinationMember>> destinationMember) {
                   // Mapgen will use this method as mapping configuration.
                 }
             """;
  }

  private const string MapperClassTemplate =
    """
    // <auto-generated />
    {{NullableDirective}}

    {{Usings}}

    namespace {{Namespace}} {
      {{ClassAccessibility}} partial class {{ClassName}} {
    {{MapperFields}}
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

  private const string MapperClassMethodWithConstructorTemplate =
    """
        {{MethodAccessibility}} partial {{ReturnType}} {{MethodName}}({{Parameters}}) {
          {{ConstructorCall}}
        }
    """;
}
