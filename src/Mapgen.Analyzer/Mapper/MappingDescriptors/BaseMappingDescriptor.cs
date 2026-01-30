namespace Mapgen.Analyzer.Mapper.MappingDescriptors;

public abstract class BaseMappingDescriptor
{
  public string TargetMemberName { get; }

  protected BaseMappingDescriptor(string targetMemberName)
  {
    TargetMemberName = targetMemberName;
  }
}

public abstract class SourceMappingDescriptor : BaseMappingDescriptor
{
  public string SourceExpression { get; }

  protected SourceMappingDescriptor(string targetMemberName, string sourceExpression)
    : base(targetMemberName)
  {
    SourceExpression = sourceExpression;
  }
}
