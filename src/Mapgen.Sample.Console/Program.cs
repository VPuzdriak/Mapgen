using System;
using System.Text.Json;

using Mapgen.Sample.Console.Mappers;
using Mapgen.Sample.Console.Models;

var carOwner = new CarOwner { FirstName = "Alice", LastName = "Johnson" };

var garage = new Garage
{
  City = "Mapgen",
  Street = "Mapping str",
  Number = 42,
  Cars =
  [
    new Car
    {
      Make = "Toyota", Model = "Corolla", ReleaseYear = 2020, Owner = carOwner,
    },
    new Car
    {
      Make = "Ford", Model = "Mustang", ReleaseYear = 2018, Owner = carOwner,
    },
    new Car
    {
      Make = "BMW", Model = "X5", ReleaseYear = 2021, Owner = carOwner,
    },
    new Car
    {
      Make = "Porsche", Model = "911", ReleaseYear = 2019, Owner = carOwner,
    },
  ],
};

var driver = new Driver { FirstName = "Bob", LastName = "Smith" };

var garageDto = garage.ToGarageDto(driver);

Console.WriteLine("Garage:");
Console.WriteLine(JsonSerializer.Serialize(garage, new JsonSerializerOptions { WriteIndented = true }));
Console.WriteLine("-------------------------------------");
Console.WriteLine("GarageDto:");
Console.WriteLine(JsonSerializer.Serialize(garageDto, new JsonSerializerOptions { WriteIndented = true }));
