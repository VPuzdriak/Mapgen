namespace Mapgen.Tests.Unit.RecordMapping.Models;

public record PersonRecord
{
  public required string Name { get; init; }
  public required int Age { get; init; }
}

public record PersonRecordDto
{
  public required string Name { get; init; }
  public required int Age { get; init; }
}

