namespace Mapgen.Analyzer.Mapper.MappingDescriptors;

/// <summary>
/// Represents a constructor argument mapping.
/// </summary>
public sealed class ConstructorArgumentDescriptor : SourceMappingDescriptor
{
  public int ParameterPosition { get; }

  public ConstructorArgumentDescriptor(
    string targetMemberName,
    string sourceExpression,
    int parameterPosition)
    : base(targetMemberName, sourceExpression)
  {
    ParameterPosition = parameterPosition;
  }
}
