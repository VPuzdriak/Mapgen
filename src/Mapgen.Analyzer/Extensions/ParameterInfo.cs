namespace Mapgen.Analyzer.Extensions;

public sealed class ParameterInfo
{
  public string Name { get; }
  public string Type { get; }

  public ParameterInfo(string name, string type)
  {
    Name = name;
    Type = type;
  }
}
