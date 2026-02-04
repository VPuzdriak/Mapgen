using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

using Mapgen.Analyzer.Mapper.Metadata;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Strategies;

/// <summary>
/// Strategy for parsing UseConstructor() and UseEmptyConstructor() configuration calls.
/// </summary>
public sealed class ConstructorMappingStrategy : BaseMappingStrategy
{
  private readonly SemanticModel _semanticModel;

  public ConstructorMappingStrategy(SemanticModel semanticModel)
  {
    _semanticModel = semanticModel;
  }

  /// <summary>
  /// Checks if the mapper has a UseConstructor() call.
  /// </summary>
  public bool HasUseConstructorCall(SyntaxNode classNode)
  {
    return HasConfigurationMethodCall(classNode, MappingConfigurationMethods.UseConstructorMethodName);
  }

  /// <summary>
  /// Checks if the mapper has a UseEmptyConstructor() call.
  /// </summary>
  public bool HasUseEmptyConstructorCall(SyntaxNode classNode)
  {
    return HasConfigurationMethodCall(classNode, MappingConfigurationMethods.UseEmptyConstructorMethodName);
  }

  /// <summary>
  /// Gets the location of the UseEmptyConstructor() call for diagnostic reporting.
  /// </summary>
  public Location? GetUseEmptyConstructorCallLocation(SyntaxNode classNode)
  {
    return GetConfigurationMethodCallLocation(classNode, MappingConfigurationMethods.UseEmptyConstructorMethodName);
  }

  /// <summary>
  /// Parses UseConstructor() call and extracts the lambda expressions for constructor arguments.
  /// </summary>
  public List<string> ParseConstructorArguments(
    SyntaxNode classNode,
    MapperMethodMetadata methodMetadata,
    CancellationToken ct)
  {
    var constructorArguments = new List<string>();

    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return constructorArguments;
    }

    // Find UseConstructor invocation
    var useConstructorCall = GetConfigurationMethodCalls(constructor, MappingConfigurationMethods.UseConstructorMethodName)
      .FirstOrDefault();

    if (useConstructorCall is null)
    {
      return constructorArguments;
    }

    // Extract each lambda argument
    foreach (var argument in useConstructorCall.ArgumentList.Arguments)
    {
      if (ct.IsCancellationRequested)
      {
        break;
      }

      // Get the lambda expression body and replace parameters
      if (argument.Expression is ParenthesizedLambdaExpressionSyntax or SimpleLambdaExpressionSyntax)
      {
        var bodyExpression = LambdaParameterReplacer.ExtractAndReplaceParameters(
          argument.Expression,
          methodMetadata,
          skipFirstLambdaParams: 0);

        if (bodyExpression != null)
        {

          constructorArguments.Add(bodyExpression);
        }
      }
    }

    return constructorArguments;
  }

  /// <summary>
  /// Checks if a constructor can be automatically mapped from source type members (properties and fields).
  /// A constructor is auto-mappable if all its parameters can be matched to source members
  /// by name (case-insensitive) and the types are compatible (same, implicitly convertible, or mappable via included mapper).
  /// </summary>
  /// <param name="constructor">The constructor to check.</param>
  /// <param name="sourceType">The source type to map from.</param>
  /// <param name="methodMetadata">The mapper method metadata containing included mappers.</param>
  /// <returns>True if all constructor parameters can be automatically mapped, false otherwise.</returns>
  public bool CanAutoMapConstructor(IMethodSymbol constructor, INamedTypeSymbol sourceType, MapperMethodMetadata methodMetadata)
  {
    if (constructor.Parameters.Length == 0)
    {
      return false; // Parameterless constructors don't need auto-mapping
    }

    var sourceMembers = sourceType.GetAllMembers().ToList();

    foreach (var parameter in constructor.Parameters)
    {
      // Find source member by name (case-insensitive match)
      var sourceMember = sourceMembers.FirstOrDefault(m =>
        string.Equals(m.Name, parameter.Name, StringComparison.OrdinalIgnoreCase));

      if (sourceMember == null)
      {
        return false; // No matching source member
      }

      // Check if member is readable
      if (!sourceMember.IsReadable())
      {
        return false; // Source member can't be read
      }

      // Check type compatibility (direct match, implicit conversion, or included mapper)
      if (!AreTypesCompatible(sourceMember.Type, parameter.Type) &&
          !CanMapViaIncludedMapper(sourceMember.Type, parameter.Type, methodMetadata))
      {
        return false; // Types are not compatible
      }
    }

    return true; // All parameters can be mapped
  }

  /// <summary>
  /// Checks if source type can be assigned to destination type (same type or implicit conversion exists).
  /// </summary>
  private bool AreTypesCompatible(ITypeSymbol sourceType, ITypeSymbol destinationType)
  {
    return TypeCompatibilityChecker.AreTypesCompatible(sourceType, destinationType, _semanticModel);
  }

  /// <summary>
  /// Checks if there's an included mapper that can map from source type to destination type.
  /// </summary>
  private static bool CanMapViaIncludedMapper(ITypeSymbol sourceType, ITypeSymbol destinationType, MapperMethodMetadata methodMetadata)
  {
    return IncludedMapperHelpers.TryFindIncludedMapperMethod(
      sourceType,
      destinationType,
      methodMetadata,
      out _,
      out _,
      out _);
  }

  /// <summary>
  /// Builds the expression for a constructor argument, using an included mapper if types don't match directly.
  /// </summary>
  public string BuildConstructorArgumentExpression(
    MemberInfo sourceMember,
    ITypeSymbol parameterType,
    MapperMethodMetadata methodMetadata)
  {
    var sourceExpression = $"{methodMetadata.SourceObjectParameter.Name}.{sourceMember.Name}";

    // If types match directly, just use the source expression
    if (AreTypesCompatible(sourceMember.Type, parameterType))
    {
      return sourceExpression;
    }

    // Try to find an included mapper that can handle the conversion
    if (IncludedMapperHelpers.TryFindIncludedMapperMethod(
          sourceMember.Type,
          parameterType,
          methodMetadata,
          out var includedMapper,
          out var mapperMethod,
          out var isCollectionMapping))
    {
      // Build the mapping expression using the found mapper
      return IncludedMapperHelpers.BuildMappingExpression(
        sourceExpression,
        sourceMember.Type,
        parameterType,
        includedMapper!,
        mapperMethod!,
        isCollectionMapping,
        methodMetadata);
    }

    // Fallback: just use the source expression (will likely cause a compilation error, but that's better than nothing)
    return sourceExpression;
  }

  /// <summary>
  /// Selects a constructor from the destination type that matches the expected parameter count.
  /// If multiple constructors have the same parameter count, returns the first one found.
  /// This is used when UseConstructor() is called to find the matching constructor signature.
  /// </summary>
  /// <param name="destinationType">The destination type to search for constructors.</param>
  /// <param name="expectedParameterCount">The number of parameters expected in the constructor.</param>
  /// <returns>
  /// The first public constructor with the matching parameter count, or null if no match is found.
  /// </returns>
  /// <remarks>
  /// Note: If there are multiple constructors with the same parameter count but different types,
  /// this method returns the first match. The generator will validate parameter types during code generation.
  /// </remarks>
  public IMethodSymbol? SelectConstructorByParameterCount(
    INamedTypeSymbol destinationType,
    int expectedParameterCount)
  {
    return destinationType.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Accessibility.Public)
      .FirstOrDefault(c => c.Parameters.Length == expectedParameterCount);
  }

  /// <summary>
  /// Gets a formatted string of all public constructor signatures for diagnostic messages.
  /// Used to show developers what constructors are available when configuration is needed.
  /// </summary>
  /// <param name="destinationType">The destination type to get constructor signatures for.</param>
  /// <returns>A formatted multi-line string with all constructor signatures.</returns>
  /// <example>
  /// Output format:
  ///   - ProductDto()
  ///   - ProductDto(string name, decimal price)
  ///   - ProductDto(string name, decimal price, int stock)
  /// </example>
  public static string GetConstructorSignatures(INamedTypeSymbol destinationType)
  {
    var constructors = destinationType.InstanceConstructors
      .Where(c => c.DeclaredAccessibility == Accessibility.Public)
      .ToList();

    var signatures = constructors.Select(c =>
    {
      if (c.Parameters.Length == 0)
      {
        return $"  - {destinationType.Name}()";
      }

      var parameters = c.Parameters
        .Select(p => $"{p.Type.ToDisplayString()} {p.Name}");

      return $"  - {destinationType.Name}({string.Join(", ", parameters)})";
    });

    return string.Join("\n", signatures);
  }

  /// <summary>
  /// Checks if the mapper constructor body contains any calls to a specific configuration method.
  /// </summary>
  private static bool HasConfigurationMethodCall(SyntaxNode classNode, string methodName)
  {
    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return false;
    }

    return GetConfigurationMethodCalls(constructor, methodName).Any();
  }

  /// <summary>
  /// Gets the location of the first call to a specific configuration method in the constructor body.
  /// Used for diagnostic reporting.
  /// </summary>
  private static Location? GetConfigurationMethodCallLocation(SyntaxNode classNode, string methodName)
  {
    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return null;
    }

    var invocation = GetConfigurationMethodCalls(constructor, methodName).FirstOrDefault();
    return invocation?.GetLocation();
  }
}
