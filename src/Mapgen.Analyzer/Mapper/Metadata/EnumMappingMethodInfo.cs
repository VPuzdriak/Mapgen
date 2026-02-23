using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata;

/// <summary>
/// Represents metadata for a static enum mapping method that needs to be generated.
/// </summary>
public sealed class EnumMappingMethodInfo
{
  public ITypeSymbol SourceEnumType { get; }
  public ITypeSymbol DestEnumType { get; }
  public string MethodName { get; }
  public bool IsSourceNullable { get; }
  public bool IsDestNullable { get; }

  public EnumMappingMethodInfo(
    ITypeSymbol sourceEnumType,
    ITypeSymbol destEnumType,
    string methodName,
    bool isSourceNullable,
    bool isDestNullable)
  {
    SourceEnumType = sourceEnumType;
    DestEnumType = destEnumType;
    MethodName = methodName;
    IsSourceNullable = isSourceNullable;
    IsDestNullable = isDestNullable;
  }
}

