using System.Collections.Generic;

using Mapgen.Analyzer.Mapper.Diagnostics;
using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata
{
  public sealed class MappingConfigurationMetadata
  {
    public IReadOnlyList<string> Usings { get; set; }
    public string MapperNamespace { get; }
    public string MapperName { get; }
    public Accessibility MapperAccessibility { get; }
    public MapperMethodMetadata? Method { get; }
    public IReadOnlyList<MapperDiagnostic> Diagnostics { get; }
    public TypeAliasResolver TypeAliasResolver { get; }

    public MappingConfigurationMetadata(
      IReadOnlyList<string> usings,
      string mapperNamespace,
      string mapperName,
      Accessibility mapperAccessibility,
      MapperMethodMetadata? method,
      IReadOnlyList<MapperDiagnostic> diagnostics)
    {
      Usings = usings;
      MapperNamespace = mapperNamespace;
      MapperName = mapperName;
      MapperAccessibility = mapperAccessibility;
      Method = method;
      Diagnostics = diagnostics;
      TypeAliasResolver = new TypeAliasResolver(usings);
    }
  }
}
