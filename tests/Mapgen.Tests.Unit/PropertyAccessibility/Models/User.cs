namespace Mapgen.Tests.Unit.PropertyAccessibility.Models;

/// <summary>
/// Domain model with encapsulated state - represents a user in the system
/// </summary>
public class User
{
  public required Guid Id { get; init; }
  public required string Username { get; set; }
  public required string Email { get; set; }

  // Password is write-only (no getter) - for security
  private string _passwordHash = "";
  public string Password
  {
    set => _passwordHash = HashPassword(value);
  }

  // Computed property - cannot be set from outside
  public string DisplayName => $"{Username} ({Email})";

  // Audit fields with private setters - managed by the system
  public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;
  public DateTime? LastLoginAt { get; private set; }

  // Internal method for password hashing
  public void UpdateLastLogin() => LastLoginAt = DateTime.UtcNow;

  private static string HashPassword(string password) => $"HASH_{password}";
}
