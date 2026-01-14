namespace Mapgen.Analyzer.Mapper.MappingDescriptors;

/// <summary>
/// Represents a constructor argument mapping.
/// </summary>
public sealed class ConstructorArgumentDescriptor : BaseMappingDescriptor
{
  public string SourceExpression { get; }
  public int ParameterPosition { get; }

  public ConstructorArgumentDescriptor(
    string targetPropertyName,
    string sourceExpression,
    int parameterPosition)
    : base(targetPropertyName)
  {
    SourceExpression = sourceExpression;
    ParameterPosition = parameterPosition;
  }
}
