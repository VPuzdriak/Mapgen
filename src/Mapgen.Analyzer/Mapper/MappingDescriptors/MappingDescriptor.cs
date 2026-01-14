namespace Mapgen.Analyzer.Mapper.MappingDescriptors
{
  public sealed class MappingDescriptor : BaseMappingDescriptor
  {
    public string SourceExpression { get; }

    public MappingDescriptor(string targetPropertyName, string sourceExpression)
      : base(targetPropertyName)
    {
      SourceExpression = sourceExpression;
    }
  }
}
