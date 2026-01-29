using System;

namespace Mapgen.Tests.Unit.PropertyAccessibility.Models;

/// <summary>
/// DTO for transferring user data to client - only publicly accessible data
/// </summary>
public class UserDto
{
  public required Guid Id { get; init; }
  public required string Username { get; set; }
  public required string Email { get; set; }

  // Computed property in DTO - cannot be mapped to
  public string DisplayName => $"{Username} ({Email})";

  // Audit fields are read-only in DTO (init-only)
  public DateTime CreatedAt { get; init; }
  public DateTime? LastLoginAt { get; init; }
}
