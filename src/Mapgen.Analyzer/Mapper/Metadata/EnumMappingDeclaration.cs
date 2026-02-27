using Microsoft.CodeAnalysis;

namespace Mapgen.Analyzer.Mapper.Metadata;

/// <summary>
/// Represents a MapEnum() declaration in the mapper constructor.
/// </summary>
public sealed class EnumMappingDeclaration
{
  public ITypeSymbol SourceEnumType { get; }
  public ITypeSymbol DestEnumType { get; }
  public Location? Location { get; }

  public EnumMappingDeclaration(ITypeSymbol sourceEnumType, ITypeSymbol destEnumType, Location? location)
  {
    SourceEnumType = sourceEnumType;
    DestEnumType = destEnumType;
    Location = location;
  }
}

