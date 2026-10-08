using Microsoft.Azure.Devices.Client;
using Microsoft.Azure.Devices.Shared;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using Telemetry_IOT.Models;

namespace Telemetry_IOT.Services
{
    public class IoTService(IConfiguration config)
    {
        private readonly IConfiguration _config = config;

        string ConnectionString => _config["IoTHub:ConnectionString"] ?? throw new Exception("Du har glömt sätta ConnectionString för IoT Hub");

        DeviceClient deviceClient => DeviceClient.CreateFromConnectionString(ConnectionString);

        public async Task SendEventData(Telemetry data)
        {
            try
            {

                // Omvandla mätvärde till JSON.
                var json = JsonSerializer.Serialize(data);

                // Formatera mätvärden på ett sätt som DeviceClient kan föra över.
                var message = new Message(Encoding.UTF8.GetBytes(json));

                //Skicka värden till IoT Hub.
                await deviceClient.SendEventAsync(message);
            }
            catch (Exception e)
            {
                Console.WriteLine($"Fel uppstod: {e.Message}");
            }
        }

        public async Task UpdateDeviceInCloud(Telemetry data)
        {
            /*
             * För att uppdatera IoT Hub Device Twin, de värden som är lagrade i enheten i Azure.
             * Varje rad är en property i enheten. För att ta bort en egenskap som redan
             * ligger i enheten så kan man tilldela den NULL när man skickar ett värde.
             * Värden som redan ligger i enheten och inte är med i listan blir inte uppdaterade
             * men ligger kvar.
            */
            /*
             * Om du vill skapa en lista med uppdateringar manuellt så ser den ut så här.
             * 
            
            var reportedProperties = new TwinCollection
            {
                ["speed"] = data.Speed,
                ["moisture"] = 100
            };


                Om du vill skapa en uppdatering ifrån en modell, parse:a som JSON-text och använd
                det för att skapa en TwinCollection.            
             */
            var json = JsonSerializer.Serialize(data);
            var propertiesFromJSON = new TwinCollection(json);

            // Skicka själva listan med värden att uppdatera.
            await deviceClient.UpdateReportedPropertiesAsync(propertiesFromJSON);

        }
    }
}
