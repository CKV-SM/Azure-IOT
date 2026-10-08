using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Telemetry_IOT.Models;

namespace Telemetry_IOT.Services;

public class DbService(IConfiguration config)
{
    private readonly IConfiguration _config = config;
    // Hämta connectionstring ifrån Secrets
    private string ConnectionString => _config["SQL:ConnectionString"] ?? throw new Exception("Du har glömt sätta ConnectionString för SQL Server");

    public async Task SendData(Telemetry data)
    {
        try
        {
            // Öppna anslutning
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            // Spara query utan variabelvärden
            var sql = """
        INSERT INTO Telemetry
        (VehicleId, Timestamp, Speed)
        VALUES (@vehicleId, @timestamp, @speed)
        """;

            //Lägg in värden säkert för att undvika parameter injection
            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@vehicleId", data.VehicleID);
            command.Parameters.AddWithValue("@timestamp", data.TimeStamp);
            command.Parameters.AddWithValue("@speed", data.Speed);

            //Skicka värden
            await command.ExecuteNonQueryAsync();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"SQL-skrivning misslyckades: {ex.Message}", ex);
        }
    }

    public async Task<List<Telemetry>> GetAll()
    {
        try
        {
            await using SqlConnection? connection = new(ConnectionString);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            var readSql = """
                SELECT VehicleId, Timestamp, Speed 
                FROM Telemetry 
                """;

            await using var readCommand = new SqlCommand(readSql, connection);

            /* För att hämta alla värden för en enstaka bil

                var sqlWithWhere = """
                    SELECT VehicleId, Timestamp, Speed 
                    FROM Telemetry 
                    WHERE VehicleId = @vehicleId
                """;

                command.Parameters.AddWithValue("@vehicleId", telemetry.VehicleId);
            */

            // 1. Använd ExecuteReaderAsync för att starta läsningen asynkront
            await using var reader = await readCommand.ExecuteReaderAsync();

            var results = new List<Telemetry>();
            // 2. Loopa igenom resultatet asynkront rad för rad
            while (await reader.ReadAsync())
            {
                // 3. Hämta ut värdena (använd korrekta datatyper för dina kolumner)
                Telemetry newValue = new()
                {
                    Speed = reader.GetInt32(reader.GetOrdinal("Speed")), // eller GetDecimal / GetInt32 beroende på SQL-typ
                    VehicleID = reader.GetString(reader.GetOrdinal("VehicleId")),
                    TimeStamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                };
                results.Add(newValue);
            }
            return results;
        }
        catch (Exception ex)
        {
            throw new($"Värden gick inte att hämta: {ex.Message}", ex);
        }
    }

    public async Task<List<Telemetry>> GetAllById(string id)
    {
        try
        {
            await using SqlConnection? connection = new(ConnectionString);
            if (connection.State != ConnectionState.Open) await connection.OpenAsync();

            /* För att hämta alla värden för en enstaka bil */

            var sql = """
                    SELECT VehicleId, Timestamp, Speed 
                    FROM Telemetry 
                    WHERE VehicleId = @vehicleId
                """;

            await using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@vehicleId", id);

            // 1. Använd ExecuteReaderAsync för att starta läsningen asynkront
            await using var reader = await command.ExecuteReaderAsync();

            var results = new List<Telemetry>();
            // 2. Loopa igenom resultatet asynkront rad för rad
            while (await reader.ReadAsync())
            {
                // 3. Hämta ut värdena (använd korrekta datatyper för dina kolumner)
                Telemetry newValue = new()
                {
                    Speed = reader.GetInt32(reader.GetOrdinal("Speed")), // eller GetDecimal / GetInt32 beroende på SQL-typ
                    VehicleID = reader.GetString(reader.GetOrdinal("VehicleId")),
                    TimeStamp = reader.GetDateTime(reader.GetOrdinal("Timestamp"))
                };
                results.Add(newValue);
            }
            return results;
        }
        catch (Exception ex)
        {
            throw new($"Värden gick inte att hämta: {ex.Message}", ex);
        }
    }

}