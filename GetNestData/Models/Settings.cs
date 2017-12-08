using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace GetNestData.Models
{
    public static class Settings
    {
        public static string NestApiKey { get; set; }
        public static string WeatherApi { get; set; }
        public static string ThermostatOneId { get; set; }
        public static string ThermostatTwoId { get; set; }
        public static string GoogleCredential { get; set; }

        public static void LoadSettings(string functionDirectory)
        {
            using (var r = new StreamReader(Path.Combine(functionDirectory, "Data", "user.settings.json")))
            {
                var dataString = r.ReadToEnd();
                dynamic data = JsonConvert.DeserializeObject(dataString);
                NestApiKey = data.nestApiKey;
                WeatherApi = data.weatherApi;
                ThermostatOneId = data.thermostatOneId;
                ThermostatTwoId = data.thermostatTwoId;
                GoogleCredential = JsonConvert.SerializeObject(data.google_credential);
            }
        }

    }
}
