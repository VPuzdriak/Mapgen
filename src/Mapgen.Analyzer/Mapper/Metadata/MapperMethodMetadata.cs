using System.Collections.Generic;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.MappingDescriptors;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Mapgen.Analyzer.Mapper.Metadata;

public sealed class MapperMethodMetadata
{
  private readonly List<MapperMethodParameter> _parameters = [];
  private readonly List<BaseMappingDescriptor> _mappings = [];
  private readonly List<MapperDiagnostic> _diagnostics = [];
  private readonly List<IncludedMapperInfo> _includedMappers = [];
  private readonly HashSet<string> _requiredUsings = [];

  public IMethodSymbol MethodSymbol { get; }
  public string ReturnTypeSyntax { get; }
  public INamedTypeSymbol ReturnType => (INamedTypeSymbol)MethodSymbol.ReturnType;
  public string ReturnTypeName => MethodSymbol.ReturnType.Name;
  public string MethodName => MethodSymbol.Name;
  public Accessibility MethodAccessibility => MethodSymbol.DeclaredAccessibility;
  public ConstructorInfo? ConstructorInfo { get; private set; }
  public bool UseEmptyConstructor { get; private set; }

  public MapperMethodParameter SourceObjectParameter =>
    _parameters.Count > 0
      ? _parameters[0]
      : throw new System.InvalidOperationException("No parameters defined for this method.");

  public IReadOnlyList<MapperMethodParameter> Parameters => _parameters;
  public IReadOnlyList<BaseMappingDescriptor> Mappings => _mappings;
  public IReadOnlyList<MapperDiagnostic> Diagnostics => _diagnostics;
  public IReadOnlyList<IncludedMapperInfo> IncludedMappers => _includedMappers;
  public IReadOnlyCollection<string> RequiredUsings => _requiredUsings;

  public MapperMethodMetadata(IMethodSymbol methodSymbol, MethodDeclarationSyntax methodDeclarationSyntax)
  {
    MethodSymbol = methodSymbol;
    ReturnTypeSyntax = methodDeclarationSyntax.ReturnType.ToString();

    for (int i = 0; i < methodSymbol.Parameters.Length; i++)
    {
      var parameter = methodSymbol.Parameters[i];

      var originalTypeSyntax = methodDeclarationSyntax.ParameterList.Parameters[i].Type!.ToString();

      var parameterMetadata = new MapperMethodParameter(parameter, originalTypeSyntax);
      _parameters.Add(parameterMetadata);
    }
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

  public void SetConstructorInfo(ConstructorInfo? constructorInfo)
  {
    ConstructorInfo = constructorInfo;
  }

  public void SetUseEmptyConstructor(bool useEmptyConstructor)
  {
    UseEmptyConstructor = useEmptyConstructor;
  }

  public void AddRequiredUsing(string ns)
  {
    _requiredUsings.Add(ns);
  }
}
