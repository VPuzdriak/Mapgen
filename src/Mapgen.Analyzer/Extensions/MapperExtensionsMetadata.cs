using System.Collections.Generic;

using Mapgen.Analyzer.Mapper.Utils;

using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Extensions
{
  public sealed class MapperExtensionsMetadata
  {
    public IReadOnlyList<string> Usings { get; }
    public string MapperNamespace { get; }
    public Accessibility MapperClassAccessibility { get; }
    public string MapperClassName { get; }
    public List<ExtensionMethodInfo> ExtensionMethods { get; }
    public TypeAliasResolver TypeAliasResolver { get; }

    public MapperExtensionsMetadata(IReadOnlyList<string> usings, string mapperNamespace, Accessibility mapperClassAccessibility, string mapperClassName, List<ExtensionMethodInfo> extensionMethods)
    {
      Usings = usings;
      MapperNamespace = mapperNamespace;
      MapperClassAccessibility = mapperClassAccessibility;
      MapperClassName = mapperClassName;
      ExtensionMethods = extensionMethods;
      TypeAliasResolver = new TypeAliasResolver(usings);
    }
  }
}
