namespace Mapgen.Analyzer.Mapper.MappingDescriptors;

public abstract class BaseMappingDescriptor
{
  public string TargetPropertyName { get; }

  protected BaseMappingDescriptor(string targetPropertyName)
  {
    TargetPropertyName = targetPropertyName;
  }
}
