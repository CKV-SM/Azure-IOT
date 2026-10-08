using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using System.Text;
using System.Text.Json;
using Telemetry_IOT.Models;
using Microsoft.Data.SqlClient;

// Hämta User Secrets
var configuration = new ConfigurationBuilder()
    .AddUserSecrets<Program>()
    .Build();

var iotConnectionString = configuration["IoTHub:ConnectionString"];

// Skapa en client för IoT-enheten mot Azure utifrån den sparade connectionstringen från IoT Hub.
var deviceClient = DeviceClient.CreateFromConnectionString(iotConnectionString);

//Skapa ett objekt med mätvärden.
var telemetryData = new Telemetry() { 
    VehicleID = "Bil7",
    Speed = 80,
    TimeStamp = DateTime.UtcNow
};

// Omvandla mätvärde till JSON.
var json = JsonSerializer.Serialize(telemetryData);

// Formatera mätvärden på ett sätt som DeviceClient kan föra över.
var message = new Message(Encoding.UTF8.GetBytes(json));

//Skicka värden till IoT Hub.
await deviceClient.SendEventAsync(message);


/*
 * För att uppdatera IoT Hub Device Twin, de värden som är lagrade i enheten i Azure.
 * Varje rad är en property i enheten. För att ta bort en egenskap som redan
 * ligger i enheten så kan man tilldela den NULL när man skickar ett värde.
 * Värden som redan ligger i enheten och inte är med i listan blir inte uppdaterade
 * men ligger kvar.
*/
var reportedProperties = new TwinCollection {
    ["speed"] = 200
};

// Skicka själva listan med värden att uppdatera.
await deviceClient.UpdateReportedPropertiesAsync(reportedProperties);


/*
 * För att skriva värden till SQL
 */

// Hämta connectionstring ifrån Secrets
var sqlConnectionString = configuration["SQL:ConnectionString"];

// Öppna anslutning
await using var connection = new SqlConnection(sqlConnectionString);
await connection.OpenAsync();

// Spara query utan variabelvärden
var sql = """
    INSERT INTO Telemetry
    (VehicleId, Timestamp, Speed)
    VALUES (@vehicleId, @timestamp, @speed)
    """;

//Lägg in värden säkert för att undvika parameter injection
await using var command = new SqlCommand(sql, connection);
command.Parameters.AddWithValue("@vehicleId", telemetryData.VehicleID);
command.Parameters.AddWithValue("@timestamp", telemetryData.TimeStamp);
command.Parameters.AddWithValue("@speed", telemetryData.Speed);

//Skicka värden
await command.ExecuteNonQueryAsync();



/*
 * 3. För att läsa tillbaka värden ifrån SQL
 */

var readSql = """
SELECT VehicleId, Timestamp, Speed 
FROM Telemetry 
""";

await using var readCommand = new SqlCommand(readSql, connection);

// 1. Använd ExecuteReaderAsync för att starta läsningen asynkront
await using var reader = await readCommand.ExecuteReaderAsync();

// 2. Loopa igenom resultatet asynkront rad för rad
while (await reader.ReadAsync())
{
    // 3. Hämta ut värdena (använd korrekta datatyper för dina kolumner)
    string vehicleId = reader.GetString(reader.GetOrdinal("VehicleId"));
    DateTime timestamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"));
    double speed = reader.GetInt32(reader.GetOrdinal("Speed")); // eller GetDecimal / GetInt32 beroende på SQL-typ

    // Här kan du göra något med datan, t.ex. lägga till i en lista
    Console.WriteLine($"Fordon: {vehicleId}, Tid: {timestamp}, Hastighet: {speed}");
}