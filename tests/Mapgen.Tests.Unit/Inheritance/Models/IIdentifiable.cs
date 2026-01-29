using System;

namespace Mapgen.Tests.Unit.Inheritance.Models;

public interface IIdentifiable
{
  Guid Id { get; }
}
