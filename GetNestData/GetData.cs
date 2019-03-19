using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using GetNestData.Models;
using Google.Apis.Auth.OAuth2;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace GetNestData
{
    public static class GetData
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        [FunctionName("GetData")]
        public static async Task Run([TimerTrigger("0 */5 * * * *")]TimerInfo myTimer, TraceWriter log, ExecutionContext context)
        {
            if (Settings.NestApiKey == null)
            {
                Settings.LoadSettings(context.FunctionAppDirectory);
            }
            await RunJob(log);
        }

        private static async Task RunJob(TraceWriter log)
        {
            var now = DateTime.UtcNow;
            var timestampRounded = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, 0);
            var timestamp = ((DateTimeOffset)timestampRounded).ToUnixTimeMilliseconds();
            var reading = await GetReading(timestamp);
            await SaveData(reading, timestamp);
        }

        private static async Task<dynamic> GetNestData()
        {
            var requestUri = new Uri("https://developer-api.nest.com/");
            var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.NestApiKey);
            var response = await HttpClient.SendAsync(request);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var finalRequestUri = response.RequestMessage.RequestUri;
                if (finalRequestUri != requestUri && finalRequestUri.Host.EndsWith(".nest.com")) // detect that a redirect actually did occur.
                {
                    var finalRequest = new HttpRequestMessage(HttpMethod.Get, finalRequestUri);
                    finalRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Settings.NestApiKey);
                    response = await HttpClient.SendAsync(finalRequest);
                }
            }
            var responseBody = await response.Content.ReadAsStringAsync();
            dynamic data = JsonConvert.DeserializeObject(responseBody);
            return data;
        }

        private static async Task<Reading> GetReading(long timestamp)
        {
            var nestData = await GetNestData();
            var thermostat = nestData.devices.thermostats[Settings.ThermostatOneId];
            var thermostatTwo = nestData.devices.thermostats[Settings.ThermostatTwoId];
            int? targetTemperature = null;
            if (thermostat.hvac_mode != "off")
            {
                targetTemperature = thermostat.hvac_mode == "heat"
                    ? thermostat.target_temperature_f
                    : thermostat.eco_temperature_low_f;
            }
            else if (thermostatTwo.hvac_mode != "off")
            {
                targetTemperature = thermostatTwo.hvac_mode == "cool"
                    ? thermostatTwo.target_temperature_f
                    : thermostatTwo.eco_temperature_high_f;
            }
            return new Reading
            {
                One = new ThermostatReading
                {
                    Temperature = thermostat.ambient_temperature_f,
                    Humidity = thermostat.humidity,
                    IsHvacOn = thermostat.hvac_state != "off"
                },
                Two = new ThermostatReading
                {
                    Temperature = thermostatTwo.ambient_temperature_f,
                    Humidity = thermostatTwo.humidity,
                    IsHvacOn = thermostatTwo.hvac_state != "off"
                },
                TargetTemperature = targetTemperature,
                Timestamp = timestamp,
                TemperatureOutside = await GetTemperatureOutside()
            };
        }

        private static async Task<string> GetGoogleCredential()
        {
            var googleCred = GoogleCredential.FromJson(Settings.GoogleCredential);
            // Add the required scopes to the Google credential
            var scoped = googleCred.CreateScoped(new List<string>
            {
                "https://www.googleapis.com/auth/firebase.database",
                "https://www.googleapis.com/auth/userinfo.email"
            });
            return await scoped.UnderlyingCredential.GetAccessTokenForRequestAsync();
        }

        private static async Task SaveData(Reading reading, long timestamp)
        {
            var token = await GetGoogleCredential();
            var requestUri = new Uri($"https://nest-eced3.firebaseio.com/readings/{timestamp}.json");
            var content = new StringContent(JsonConvert.SerializeObject(reading, new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            }), Encoding.UTF8, "application/json");
            var request = new HttpRequestMessage(HttpMethod.Put, requestUri) {Content = content};
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            await HttpClient.SendAsync(request);
        }

        private static async Task<double?> GetTemperatureOutside()
        {
            try
            {
                var response = await HttpClient.GetAsync(Settings.WeatherApi);
                var responseBody = await response.Content.ReadAsStringAsync();
                dynamic data = JsonConvert.DeserializeObject(responseBody);
                return data.currently.temperature;
            }
            catch (Exception)
            {
                return null;
            }
        }

    }

}
