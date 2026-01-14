using System.Collections.Generic;
using System.Linq;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.MappingDescriptors;
using Mapgen.Analyzer.Mapper.Metadata;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Mapgen.Analyzer.Mapper.Strategies;

public sealed class DirectMappingStrategy(SemanticModel semanticModel)
{
  public BaseMappingDescriptor TryCreateDirectMapping(
    IPropertySymbol sourcePropertySymbol,
    IPropertySymbol destPropertySymbol,
    MapperMethodMetadata methodMetadata)
  {
    // Check if types are compatible (exact match including nullable annotations or implicit conversion exists)
    var typesMatchIgnoringNullability =
      SymbolEqualityComparer.Default.Equals(sourcePropertySymbol.Type, destPropertySymbol.Type);
    var nullabilityMatches = sourcePropertySymbol.Type.NullableAnnotation == destPropertySymbol.Type.NullableAnnotation;
    var typesMatch = typesMatchIgnoringNullability && nullabilityMatches;

    if (typesMatch)
    {
      // Exact match - create direct mapping
      return CreateSuccessfulMapping(sourcePropertySymbol, destPropertySymbol, methodMetadata);
    }

    // Types don't match - check for special cases
    if (IsNullableToNonNullableMismatch(sourcePropertySymbol, destPropertySymbol, typesMatchIgnoringNullability))
    {
      return CreateNullableMismatchDiagnostic(sourcePropertySymbol, destPropertySymbol, methodMetadata);
    }

    // Check if there's an implicit conversion from source to destination type (for different types like short -> int)
    if (HasImplicitConversion(sourcePropertySymbol.Type, destPropertySymbol.Type))
    {
      return CreateSuccessfulMapping(sourcePropertySymbol, destPropertySymbol, methodMetadata);
    }

    // Check if there's an included mapper that can map from source type to destination type
    var includedMapperMapping =
      TryCreateMappingWithIncludedMapper(sourcePropertySymbol, destPropertySymbol, methodMetadata);
    if (includedMapperMapping is not null)
    {
      return includedMapperMapping;
    }

    // No valid mapping possible - report type mismatch
    return CreateTypeMismatchDiagnostic(sourcePropertySymbol, destPropertySymbol, methodMetadata);
  }

  private bool IsNullableToNonNullableMismatch(
    IPropertySymbol sourcePropertySymbol,
    IPropertySymbol destPropertySymbol,
    bool typesMatchIgnoringNullability)
  {
    if (!typesMatchIgnoringNullability)
    {
      return false;
    }

    var sourceIsNullable = sourcePropertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;
    var destIsNullable = destPropertySymbol.Type.NullableAnnotation == NullableAnnotation.Annotated;

    return sourceIsNullable && !destIsNullable;
  }

  private bool HasImplicitConversion(ITypeSymbol sourceType, ITypeSymbol destType)
  {
    if (semanticModel.Compilation is not CSharpCompilation csharpCompilation)
    {
      return false;
    }

    var conversion = csharpCompilation.ClassifyConversion(sourceType, destType);
    return conversion is { IsImplicit: true, IsUserDefined: false };
  }

  private MappingDescriptor? TryCreateMappingWithIncludedMapper(
    IPropertySymbol sourcePropertySymbol,
    IPropertySymbol destPropertySymbol,
    MapperMethodMetadata methodMetadata)
  {
    // Extract element types if these are collection types
    var sourceElementType = GetCollectionElementType(sourcePropertySymbol.Type);
    var destElementType = GetCollectionElementType(destPropertySymbol.Type);

    // Check for collection/non-collection mismatch
    var sourceIsCollection = sourceElementType != null;
    var destIsCollection = destElementType != null;

    if (sourceIsCollection != destIsCollection)
    {
      // One is a collection and the other is not - this is a type mismatch
      return null;
    }

    var isCollectionMapping = sourceElementType != null && destElementType != null;

    // Use element types for collection mapping, otherwise use the property types directly
    var sourceTypeToMatch = sourceElementType ?? sourcePropertySymbol.Type;
    var destTypeToMatch = destElementType ?? destPropertySymbol.Type;

    // Look through included mappers to find one that can map from source type to destination type
    foreach (var includedMapper in methodMetadata.IncludedMappers)
    {
      // Find the ToDto (or similar) method in the included mapper
      var mapperMethods = includedMapper.MapperType.GetMembers()
        .OfType<IMethodSymbol>()
        .Where(m => m.IsPartialDefinition && m.MethodKind == MethodKind.Ordinary)
        .ToList();

      foreach (var mapperMethod in mapperMethods)
      {
        // Check if the mapper method returns the destination type and takes the source type as first parameter
        var returnsDestType = SymbolEqualityComparer.Default.Equals(mapperMethod.ReturnType, destTypeToMatch);

        if (!returnsDestType || mapperMethod.Parameters.Length == 0)
        {
          continue;
        }

        var firstParamMatchesSourceType = SymbolEqualityComparer.Default.Equals(
          mapperMethod.Parameters[0].Type,
          sourceTypeToMatch);

        if (!firstParamMatchesSourceType)
        {
          continue;
        }

        // Found a matching mapper method! Now build the mapping expression
        var mapperFieldName = includedMapper.FieldName;
        var methodName = mapperMethod.Name;
        var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourcePropertySymbol.Name}";

        if (isCollectionMapping)
        {
          // For collections, generate: source.Property.Select(item => _mapper.Method(item, ...)).ToList() or .ToArray()
          var itemParamName = GetItemParameterName(sourceTypeToMatch);

          // Build the parameters for the mapper method call
          var methodParams = new List<string> { itemParamName };

          // Add additional parameters if the mapper method requires them (from the method metadata parameters)
          for (var i = 1; i < mapperMethod.Parameters.Length && i < methodMetadata.Parameters.Count; i++)
          {
            methodParams.Add(methodMetadata.Parameters[i].Name);
          }

          // Determine the appropriate collection conversion method based on destination type
          var conversionMethod = GetCollectionConversionMethod(destPropertySymbol.Type);

          var mappingExpression =
            $"{sourceExpression}.Select({itemParamName} => {mapperFieldName}.{methodName}({string.Join(", ", methodParams)})).{conversionMethod}()";
          return new MappingDescriptor(destPropertySymbol.Name, mappingExpression);
        }
        else
        {
          // For direct mapping, generate: _mapper.Method(source.Property, ...)
          var methodParams = new List<string> { sourceExpression };

          // Add additional parameters if the mapper method requires them (from the method metadata parameters)
          for (var i = 1; i < mapperMethod.Parameters.Length && i < methodMetadata.Parameters.Count; i++)
          {
            methodParams.Add(methodMetadata.Parameters[i].Name);
          }

          var mappingExpression = $"{mapperFieldName}.{methodName}({string.Join(", ", methodParams)})";
          return new MappingDescriptor(destPropertySymbol.Name, mappingExpression);
        }
      }
    }

    return null;
  }

  private ITypeSymbol? GetCollectionElementType(ITypeSymbol type)
  {
    // Check if this is an array type like Car[]
    if (type is IArrayTypeSymbol arrayType)
    {
      return arrayType.ElementType;
    }

    // Check if this is a generic collection type like List<T>, IEnumerable<T>, etc.
    if (type is INamedTypeSymbol { IsGenericType: true } namedType)
    {
      // Check if it's a collection type (List, IEnumerable, ICollection, etc.)
      var typeDefinition = namedType.OriginalDefinition;
      var typeName = typeDefinition.ToDisplayString();

      if (typeName.StartsWith("System.Collections.Generic.List<") ||
          typeName.StartsWith("System.Collections.Generic.IEnumerable<") ||
          typeName.StartsWith("System.Collections.Generic.ICollection<") ||
          typeName.StartsWith("System.Collections.Generic.IList<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyCollection<") ||
          typeName.StartsWith("System.Collections.Generic.HashSet<") ||
          typeName.StartsWith("System.Collections.Immutable.IImmutableList<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableList<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableArray<") ||
          typeName.StartsWith("System.Collections.Immutable.IImmutableSet<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableHashSet<"))
      {
        // Return the first generic type argument (the element type)
        return namedType.TypeArguments.Length > 0 ? namedType.TypeArguments[0] : null;
      }
    }

    return null;
  }

  private string GetItemParameterName(ITypeSymbol elementType)
  {
    // Generate a lowercase parameter name from the type name
    var typeName = elementType.Name;
    return char.ToLowerInvariant(typeName[0]) + typeName.Substring(1);
  }

  private string GetCollectionConversionMethod(ITypeSymbol destinationType)
  {
    // If destination is an array, use ToArray()
    if (destinationType is IArrayTypeSymbol)
    {
      return "ToArray";
    }

    // If destination is a generic collection type, determine the appropriate method
    if (destinationType is INamedTypeSymbol namedType)
    {
      var typeDefinition = namedType.OriginalDefinition;
      var typeName = typeDefinition.ToDisplayString();

      // For HashSet<T>, use ToHashSet()
      if (typeName.StartsWith("System.Collections.Generic.HashSet<"))
      {
        return "ToHashSet";
      }

      // For immutable collections, use the appropriate ToImmutableXxx() method
      if (typeName.StartsWith("System.Collections.Immutable.ImmutableArray<"))
      {
        return "ToImmutableArray";
      }

      if (typeName.StartsWith("System.Collections.Immutable.IImmutableList<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableList<"))
      {
        return "ToImmutableList";
      }

      if (typeName.StartsWith("System.Collections.Immutable.IImmutableSet<") ||
          typeName.StartsWith("System.Collections.Immutable.ImmutableHashSet<"))
      {
        return "ToImmutableHashSet";
      }

      // For List<T>, IList<T>, ICollection<T>, IEnumerable<T>, IReadOnlyList<T>, IReadOnlyCollection<T>, use ToList()
      if (typeName.StartsWith("System.Collections.Generic.List<") ||
          typeName.StartsWith("System.Collections.Generic.IList<") ||
          typeName.StartsWith("System.Collections.Generic.ICollection<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyList<") ||
          typeName.StartsWith("System.Collections.Generic.IReadOnlyCollection<") ||
          typeName.StartsWith("System.Collections.Generic.IEnumerable<"))
      {
        return "ToList";
      }
    }

    // Default to ToList()
    return "ToList";
  }

  private MappingDescriptor CreateSuccessfulMapping(
    IPropertySymbol sourcePropertySymbol,
    IPropertySymbol destPropertySymbol,
    MapperMethodMetadata methodMetadata)
  {
    var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourcePropertySymbol.Name}";
    return new MappingDescriptor(destPropertySymbol.Name, sourceExpression);
  }

  private DiagnosedPropertyDescriptor CreateNullableMismatchDiagnostic(
    IPropertySymbol sourcePropertySymbol,
    IPropertySymbol destPropertySymbol,
    MapperMethodMetadata methodMetadata)
  {
    var diagnostic = MapperDiagnostic.NullableToNonNullableMismatch(
      methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
      methodMetadata.ReturnTypeName,
      destPropertySymbol.Name,
      destPropertySymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      methodMetadata.SourceObjectParameter.Symbol.Type.Name,
      sourcePropertySymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      Constants.MapMemberMethodName);

    methodMetadata.AddDiagnostic(diagnostic);
    return new DiagnosedPropertyDescriptor(destPropertySymbol.Name);
  }

  private DiagnosedPropertyDescriptor CreateTypeMismatchDiagnostic(
    IPropertySymbol sourcePropertySymbol,
    IPropertySymbol destPropertySymbol,
    MapperMethodMetadata methodMetadata)
  {
    var diagnostic = MapperDiagnostic.TypeMismatchInDirectMapping(
      methodMetadata.MethodSymbol.Locations.FirstOrDefault(),
      methodMetadata.ReturnTypeName,
      destPropertySymbol.Name,
      destPropertySymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      methodMetadata.SourceObjectParameter.Symbol.Type.Name,
      sourcePropertySymbol.Type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat),
      Constants.MapMemberMethodName);

    methodMetadata.AddDiagnostic(diagnostic);
    return new DiagnosedPropertyDescriptor(destPropertySymbol.Name);
  }
}
