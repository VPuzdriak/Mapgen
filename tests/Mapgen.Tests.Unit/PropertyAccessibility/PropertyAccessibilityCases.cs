using System;

using FluentAssertions;

using Mapgen.Tests.Unit.PropertyAccessibility.Models;

namespace Mapgen.Tests.Unit.PropertyAccessibility;

public class PropertyAccessibilityCases
{
  [Fact]
  public void UserMapper_ShouldMapAccessibleProperties_AndIgnoreWriteOnlyPassword()
  {
    // Arrange: User with write-only Password (no getter)  
    var mapper = new UserAccessibilityMapper();
    var userId = Guid.NewGuid();
    var user = new User
    {
      Id = userId,
      Username = "john.doe",
      Email = "john@example.com",
      Password = "SecurePassword123" // Write-only property - no getter
    };
    user.UpdateLastLogin();

    // Act
    var dto = mapper.ToDto(user);

    // Assert: All readable properties are mapped
    dto.Id.Should().Be(userId);
    dto.Username.Should().Be("john.doe");
    dto.Email.Should().Be("john@example.com");

    // CreatedAt and LastLoginAt have public getters (can be read) and DTO has init (can be written)
    dto.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    dto.LastLoginAt.Should().NotBeNull("was set via UpdateLastLogin method");

    // Password is NOT in DTO - write-only properties cannot be read
  }

  [Fact]
  public void ProductMapper_ShouldMapReadableProperties_IncludingExpressionBodiedOnes()
  {
    // Arrange: Product with expression-bodied properties
    var mapper = new ProductAccessibilityMapper();
    var product = new Product
    {
      Sku = "LAPTOP-001",
      Name = "Professional Laptop",
      BasePrice = 1000,
      TaxRate = 0.10m
    };
    product.AddStock(50);
    product.ApplyDiscount(0.05m);

    // Act
    var dto = mapper.ToDto(product);

    // Assert: Regular properties
    dto.Sku.Should().Be("LAPTOP-001");
    dto.Name.Should().Be("Professional Laptop");
    dto.BasePrice.Should().Be(1000);
    dto.TaxRate.Should().Be(0.10m);

    // Expression-bodied properties CAN be read (have implicit getter)
    dto.TotalPrice.Should().Be(1100, "TotalPrice is expression-bodied with implicit getter");
    dto.StockLevel.Should().Be(50, "StockLevel is expression-bodied with implicit getter");

    // FormattedPrice in DTO is expression-bodied (no setter) - uses its own computed value
    dto.FormattedPrice.Should().Be("$1100,00");
  }

  [Fact]
  public void OrderMapper_ShouldMapPropertiesWithPublicGetters_IgnoringPrivateSetters()
  {
    // Arrange: Order with private setters
    var mapper = new OrderAccessibilityMapper();
    var customerId = Guid.NewGuid();
    var order = new Order
    {
      OrderNumber = "ORD-2026-001",
      CustomerId = customerId,
      OrderDate = new DateTime(2026, 1, 28)
    };
    order.UpdateTotal(500.00m);
    order.UpdateStatus("Shipped");
    order.ShippingAddress = "123 Main St, New York";
    order.TrackingNumber = "TRACK-XYZ-789";

    // Act
    var dto = mapper.ToDto(order);

    // Assert: Immutable fields
    dto.OrderNumber.Should().Be("ORD-2026-001");
    dto.CustomerId.Should().Be(customerId);
    dto.OrderDate.Should().Be(new DateTime(2026, 1, 28));

    // Mutable fields with public getters/setters
    dto.ShippingAddress.Should().Be("123 Main St, New York");
    dto.TrackingNumber.Should().Be("TRACK-XYZ-789");

    // TotalAmount: expression-bodied in source (public getter) - CAN be read
    dto.TotalAmount.Should().Be(500.00m);

    // Status: has private setter in source but public getter - CAN be read
    dto.Status.Should().Be("Shipped");

    // OrderSummary in DTO: expression-bodied (no setter) - NOT mapped, computed from other fields
    dto.OrderSummary.Should().Be("Order ORD-2026-001 - Shipped");
  }

  [Fact]
  public void Mapper_ShouldHandleNullablePropertiesCorrectly()
  {
    // Arrange: User without LastLoginAt set
    var mapper = new UserAccessibilityMapper();
    var user = new User
    {
      Id = Guid.NewGuid(),
      Username = "new.user",
      Email = "new@example.com"
    };
    // LastLoginAt is not set, remains null

    // Act
    var dto = mapper.ToDto(user);

    // Assert: Nullable property is correctly mapped as null
    dto.LastLoginAt.Should().BeNull();
  }

  [Fact]
  public void Mapper_OnlyMapsPropertiesWithPublicOrInternalAccessors()
  {
    // This test verifies the mapping respects C# visibility rules
    // - Source properties need public/internal getters
    // - Destination properties need public/internal setters or init

    var mapper = new UserAccessibilityMapper();
    var user = new User
    {
      Id = Guid.NewGuid(),
      Username = "test.user",
      Email = "test@test.com",
      Password = "this value cannot be read" // No getter
    };

    var dto = mapper.ToDto(user);

    // All properties with public getters are mapped
    dto.Username.Should().Be("test.user");
    dto.Email.Should().Be("test@test.com");

    // Password is write-only - not accessible for reading
    // DisplayName is expression-bodied in DTO - not accessible for writing
    dto.Should().NotBeNull("mapper should successfully create DTO despite inaccessible properties");
  }
}
