namespace Mapgen.Analyzer.Mapper.MappingDescriptors;

public abstract class BaseMappingDescriptor
{
  public string TargetPropertyName { get; }

  protected BaseMappingDescriptor(string targetPropertyName)
  {
    TargetPropertyName = targetPropertyName;
  }
}

public abstract class SourceMappingDescriptor : BaseMappingDescriptor
{
  public string SourceExpression { get; }

  protected SourceMappingDescriptor(string targetPropertyName, string sourceExpression)
    : base(targetPropertyName)
  {
    SourceExpression = sourceExpression;
  }
}
