using Microsoft.Extensions.Configuration;
using Telemetry_IOT.Models;
using Telemetry_IOT.Services;

/*
 * För att köra så krävs Configurationstrings för IoTHub och en Azure SQL-server i User Secrets.
 * Strukturen "IoTHub:ConnectionString" och "SQL:ConnectionString" används.
 * */

// Hämta User Secrets
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build() ?? throw new Exception("Secrets går inte att nå");

IoTService iotService = new(configuration);
DbService dbService = new(configuration);

var telemetryData = new Telemetry()
{
    VehicleID = "Bil7",
    Speed = 80,
    TimeStamp = DateTime.UtcNow
};

await iotService.SendEventData(telemetryData);
await iotService.UpdateDeviceInCloud(telemetryData);

/*
 * För att skriva värden till SQL
 */
try
{
    await dbService.SendData(telemetryData);
}
catch (Exception ex) {
    Console.WriteLine($"Det gick inte att skriva: {ex.Message}");
}

/*
 * 3. För att läsa tillbaka värden ifrån SQL
 */
try
{
    List<Telemetry> databaseValues = await dbService.GetAll();
    //List<Telemetry> databaseValuesForSpecificCar = await dbService.GetAllById("Bil7");
}
catch (Exception ex) {
    Console.WriteLine($"Det gick inte att läsa: {ex.Message}");
}