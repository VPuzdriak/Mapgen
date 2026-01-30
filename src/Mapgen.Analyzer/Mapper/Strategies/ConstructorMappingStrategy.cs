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
    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return false;
    }

    return FindMethodInvocations(constructor, Constants.UseConstructorMethodName).Any();
  }

  /// <summary>
  /// Checks if the mapper has a UseEmptyConstructor() call.
  /// </summary>
  public bool HasUseEmptyConstructorCall(SyntaxNode classNode)
  {
    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return false;
    }

    return FindMethodInvocations(constructor, Constants.UseEmptyConstructorMethodName).Any();
  }

  /// <summary>
  /// Gets the location of the UseEmptyConstructor() call for diagnostic reporting.
  /// </summary>
  public Location? GetUseEmptyConstructorCallLocation(SyntaxNode classNode)
  {
    var constructor = GetMapperConstructor(classNode);
    if (constructor is null)
    {
      return null;
    }

    var useEmptyConstructorCall = FindMethodInvocations(constructor, Constants.UseEmptyConstructorMethodName)
      .FirstOrDefault();

    return useEmptyConstructorCall?.GetLocation();
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
    var useConstructorCall = FindMethodInvocations(constructor, Constants.UseConstructorMethodName)
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
  /// by name (case-insensitive) and the types are compatible (same or implicitly convertible).
  /// </summary>
  /// <param name="constructor">The constructor to check.</param>
  /// <param name="sourceType">The source type to map from.</param>
  /// <returns>True if all constructor parameters can be automatically mapped, false otherwise.</returns>
  public bool CanAutoMapConstructor(IMethodSymbol constructor, INamedTypeSymbol sourceType)
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

      // Check type compatibility
      if (!AreTypesCompatible(sourceMember.Type, parameter.Type))
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
    // Exact match (including nullable annotations)
    if (SymbolEqualityComparer.Default.Equals(sourceType, destinationType))
    {
      return true;
    }

    // Check for implicit conversion
    if (_semanticModel.Compilation is Microsoft.CodeAnalysis.CSharp.CSharpCompilation csharpCompilation)
    {
      var conversion = csharpCompilation.ClassifyConversion(sourceType, destinationType);
      return conversion is { IsImplicit: true, IsUserDefined: false };
    }

    return false;
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
}
