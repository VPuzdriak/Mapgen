using System.Collections.Generic;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.MappingDescriptors;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata;

public sealed class MapperMethodMetadata
{
  private readonly List<MapperMethodParameter> _parameters = [];
  private readonly List<BaseMappingDescriptor> _mappings = [];
  private readonly List<MapperDiagnostic> _diagnostics = [];
  private readonly List<IncludedMapperInfo> _includedMappers = [];

  public IMethodSymbol MethodSymbol { get; }
  public INamedTypeSymbol ReturnType => (INamedTypeSymbol)MethodSymbol.ReturnType;
  public string ReturnTypeName => MethodSymbol.ReturnType.Name;
  public string MethodName => MethodSymbol.Name;
  public Accessibility MethodAccessibility => MethodSymbol.DeclaredAccessibility;

  public MapperMethodParameter SourceObjectParameter =>
    _parameters.Count > 0
      ? _parameters[0]
      : throw new System.InvalidOperationException("No parameters defined for this method.");

  public IReadOnlyList<MapperMethodParameter> Parameters => _parameters;
  public IReadOnlyList<BaseMappingDescriptor> Mappings => _mappings;
  public IReadOnlyList<MapperDiagnostic> Diagnostics => _diagnostics;
  public IReadOnlyList<IncludedMapperInfo> IncludedMappers => _includedMappers;

  public MapperMethodMetadata(IMethodSymbol methodSymbol)
  {
    MethodSymbol = methodSymbol;

    foreach (var parameter in methodSymbol.Parameters)
    {
      var parameterMetadata = new MapperMethodParameter(parameter);
      AddParameter(parameterMetadata);
    }
  }

  private static string GetAccessibilityString(Accessibility accessibility) =>
    accessibility switch
    {
      Accessibility.Public => "public",
      Accessibility.Private => "private",
      Accessibility.Protected => "protected",
      Accessibility.Internal => "internal",
      Accessibility.ProtectedOrInternal => "protected internal",
      Accessibility.ProtectedAndInternal => "private protected",
      _ => "public"
    };

  private void AddParameter(MapperMethodParameter parameter)
  {
    _parameters.Add(parameter);
  }

  public void AddMapping(BaseMappingDescriptor mapping)
  {
    _mappings.Add(mapping);
  }

  public void AddDiagnostic(MapperDiagnostic diagnostic)
  {
    _diagnostics.Add(diagnostic);
  }

  public void AddIncludedMapper(IncludedMapperInfo includedMapper)
  {
    _includedMappers.Add(includedMapper);
  }
}
