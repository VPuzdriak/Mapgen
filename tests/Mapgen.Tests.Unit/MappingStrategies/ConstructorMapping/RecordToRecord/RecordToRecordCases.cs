using FluentAssertions;

using Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord.Models;

namespace Mapgen.Tests.Unit.MappingStrategies.ConstructorMapping.RecordToRecord;

/// <summary>
/// Tests for constructor mapping with records (record to record)
/// </summary>
public class RecordToRecordCases
{
  #region Basic Record Mapping Tests

  [Fact]
  public void When_MappingRecordWithPrimaryConstructor_Should_MapConstructorParameters()
  {
    // Arrange
    var person = new PersonRecord("John", "Doe", 30)
    {
      Email = "john.doe@example.com",
      Phone = "+1-555-1234"
    };
    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result = mapper.ToDto(person);

    // Assert
    result.FirstName.Should().Be(person.FirstName);
    result.LastName.Should().Be(person.LastName);
    result.Age.Should().Be(person.Age);
    result.Email.Should().Be(person.Email);
  }

  [Fact]
  public void When_MappingRecordWithPrimaryConstructor_Should_MapRemainingPropertiesViaInitializer()
  {
    // Arrange
    var person = new PersonRecord("Jane", "Smith", 25)
    {
      Email = "jane.smith@example.com",
      Phone = "+1-555-5678"
    };
    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result = mapper.ToDto(person);

    // Assert - Phone should be mapped via object initializer (required property)
    result.Phone.Should().Be(person.Phone);
  }

  [Fact]
  public void When_MappingSimpleRecord_Should_MapAllPropertiesViaConstructor()
  {
    // Arrange
    var address = new AddressRecord("123 Main St", "Springfield", "12345");
    var mapper = new AddressRecordMapper();

    // Act
    var result = mapper.ToDto(address);

    // Assert
    result.Street.Should().Be(address.Street);
    result.City.Should().Be(address.City);
    result.ZipCode.Should().Be(address.ZipCode);
  }

  #endregion

  #region Record Immutability Tests

  [Fact]
  public void When_MappingRecord_Should_CreateImmutableDestination()
  {
    // Arrange
    var address = new AddressRecord("456 Oak Ave", "Portland", "97201");
    var mapper = new AddressRecordMapper();

    // Act
    var result = mapper.ToDto(address);

    // Assert - Record properties should be readonly (immutable)
    result.Street.Should().Be("456 Oak Ave");
    // Note: Records are immutable by default - properties set via primary constructor are readonly
  }

  [Fact]
  public void When_MappingSameRecordMultipleTimes_Should_CreateNewInstancesEachTime()
  {
    // Arrange
    var person = new PersonRecord("Bob", "Johnson", 40)
    {
      Email = "bob@example.com",
      Phone = "+1-555-9999"
    };
    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result1 = mapper.ToDto(person);
    var result2 = mapper.ToDto(person);

    // Assert - Should be different instances (records support value equality)
    result1.Should().NotBeSameAs(result2);
    result1.Should().Be(result2); // Value equality
  }

  #endregion

  #region Record Equality Tests

  [Fact]
  public void When_MappingRecordsWithSameValues_Should_BeEqual()
  {
    // Arrange
    var person1 = new PersonRecord("Alice", "Williams", 35)
    {
      Email = "alice@example.com",
      Phone = "+1-555-1111"
    };

    var person2 = new PersonRecord("Alice", "Williams", 35)
    {
      Email = "alice@example.com",
      Phone = "+1-555-1111"
    };

    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result1 = mapper.ToDto(person1);
    var result2 = mapper.ToDto(person2);

    // Assert - Records with same values should be equal (value-based equality)
    result1.Should().Be(result2);
    result1.GetHashCode().Should().Be(result2.GetHashCode());
  }

  [Fact]
  public void When_MappingRecordsWithDifferentValues_Should_NotBeEqual()
  {
    // Arrange
    var person1 = new PersonRecord("Charlie", "Brown", 28)
    {
      Email = "charlie@example.com",
      Phone = "+1-555-2222"
    };

    var person2 = new PersonRecord("Charlie", "Brown", 29) // Different age
    {
      Email = "charlie@example.com",
      Phone = "+1-555-2222"
    };

    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result1 = mapper.ToDto(person1);
    var result2 = mapper.ToDto(person2);

    // Assert - Different values should not be equal
    result1.Should().NotBe(result2);
  }

  #endregion

  #region Record Computed Property Tests

  [Fact]
  public void When_MappingRecord_Should_ComputedPropertiesWorkCorrectly()
  {
    // Arrange
    var person = new PersonRecord("David", "Miller", 45)
    {
      Email = "david@example.com",
      Phone = "+1-555-3333"
    };
    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result = mapper.ToDto(person);

    // Assert - FullName is a computed property
    result.FullName.Should().Be("David Miller");
  }

  #endregion

  #region Edge Cases

  [Fact]
  public void When_MappingRecordWithEmptyStrings_Should_PreserveEmptyValues()
  {
    // Arrange
    var person = new PersonRecord(string.Empty, string.Empty, 0)
    {
      Email = string.Empty,
      Phone = string.Empty
    };
    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result = mapper.ToDto(person);

    // Assert
    result.FirstName.Should().Be(string.Empty);
    result.LastName.Should().Be(string.Empty);
    result.Age.Should().Be(0);
    result.Email.Should().Be(string.Empty);
    result.Phone.Should().Be(string.Empty);
  }

  [Fact]
  public void When_MappingRecordWithSpecialCharacters_Should_PreserveValues()
  {
    // Arrange
    var person = new PersonRecord("John-Paul", "O'Connor", 33)
    {
      Email = "jp.oconnor+test@example.com",
      Phone = "+1-(555)-4444"
    };
    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result = mapper.ToDto(person);

    // Assert
    result.FirstName.Should().Be("John-Paul");
    result.LastName.Should().Be("O'Connor");
    result.Email.Should().Be("jp.oconnor+test@example.com");
    result.Phone.Should().Be("+1-(555)-4444");
  }

  [Fact]
  public void When_MappingRecordWithNegativeAge_Should_PreserveValue()
  {
    // Arrange (edge case - negative age)
    var person = new PersonRecord("Test", "User", -1)
    {
      Email = "test@example.com",
      Phone = "+1-555-5555"
    };
    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result = mapper.ToDto(person);

    // Assert
    result.Age.Should().Be(-1);
  }

  [Fact]
  public void When_MappingAddressRecordWithLongStrings_Should_PreserveValues()
  {
    // Arrange
    var longStreet = new string('A', 1000);
    var longCity = new string('B', 500);
    var longZip = new string('1', 100);
    var address = new AddressRecord(longStreet, longCity, longZip);
    var mapper = new AddressRecordMapper();

    // Act
    var result = mapper.ToDto(address);

    // Assert
    result.Street.Should().Be(longStreet);
    result.City.Should().Be(longCity);
    result.ZipCode.Should().Be(longZip);
  }

  #endregion

  #region Record With Statement Tests (Conceptual)

  [Fact]
  public void When_MappingRecord_ResultCanBeUsedWithWithExpression()
  {
    // Arrange
    var person = new PersonRecord("Emma", "Davis", 27)
    {
      Email = "emma@example.com",
      Phone = "+1-555-6666"
    };
    var mapper = new RecordToRecordPersonMapper();

    // Act
    var result = mapper.ToDto(person);
    var modified = result with { Age = 28 }; // Using record 'with' expression

    // Assert
    result.Age.Should().Be(27);
    modified.Age.Should().Be(28);
    modified.FirstName.Should().Be(result.FirstName);
    modified.LastName.Should().Be(result.LastName);
  }

  #endregion

  #region Multiple Instance Tests

  [Fact]
  public void When_MappingMultipleRecords_Should_CreateSeparateInstances()
  {
    // Arrange
    var address1 = new AddressRecord("100 First St", "Boston", "02101");
    var address2 = new AddressRecord("200 Second Ave", "Seattle", "98101");
    var mapper = new AddressRecordMapper();

    // Act
    var result1 = mapper.ToDto(address1);
    var result2 = mapper.ToDto(address2);

    // Assert
    result1.Should().NotBeSameAs(result2);
    result1.Should().NotBe(result2); // Different values
    result1.Street.Should().Be("100 First St");
    result2.Street.Should().Be("200 Second Ave");
  }

  #endregion
}
