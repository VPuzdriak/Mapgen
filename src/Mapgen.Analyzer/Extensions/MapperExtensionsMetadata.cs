using System.Collections.Generic;

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
    public bool NullableEnabled { get; }

    public MapperExtensionsMetadata(IReadOnlyList<string> usings, string mapperNamespace, Accessibility mapperClassAccessibility, string mapperClassName, List<ExtensionMethodInfo> extensionMethods, bool nullableEnabled)
    {
      Usings = usings;
      MapperNamespace = mapperNamespace;
      MapperClassAccessibility = mapperClassAccessibility;
      MapperClassName = mapperClassName;
      ExtensionMethods = extensionMethods;
      NullableEnabled = nullableEnabled;
    }
  }
}
