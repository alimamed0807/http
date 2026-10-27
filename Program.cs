
using System;
using System.Net.Http;
using System.Text.Json;

class Program
{
    static async Task Main()
    {
        using HttpClient client = new HttpClient();

        while (true)
        {
            Console.Write("Seherin adini daxil et (exit - cixis): ");
            string city = Console.ReadLine() ?? "";

            if (city.Trim().ToLower() == "exit")
                break;

            if (string.IsNullOrWhiteSpace(city))
            {
                Console.WriteLine("Seher adi bos ola bilmez.");
                continue;
            }

            try
            {
                string geoUrl = $"https://geocoding-api.open-meteo.com/v1/search?name={Uri.EscapeDataString(city)}&count=1&language=az&format=json";

                HttpResponseMessage geoResponse = await client.GetAsync(geoUrl);
                geoResponse.EnsureSuccessStatusCode();

                string geoJson = await geoResponse.Content.ReadAsStringAsync();
                using JsonDocument geoDoc = JsonDocument.Parse(geoJson);

                if (!geoDoc.RootElement.TryGetProperty("results", out JsonElement results) ||
                    results.GetArrayLength() == 0)
                {
                    Console.WriteLine("Seher tapilmadi.");
                    continue;
                }

                JsonElement place = results[0];

                double latitude = place.GetProperty("latitude").GetDouble();
                double longitude = place.GetProperty("longitude").GetDouble();
                string cityName = place.GetProperty("name").GetString() ?? city;

                string weatherUrl =
                    $"https://api.open-meteo.com/v1/forecast?latitude={latitude}&longitude={longitude}&current=temperature_2m,relative_humidity_2m,apparent_temperature,weather_code,wind_speed_10m&timezone=auto";

                HttpResponseMessage weatherResponse =
                    await client.GetAsync(weatherUrl);

                weatherResponse.EnsureSuccessStatusCode();

                string weatherJson =
                    await weatherResponse.Content.ReadAsStringAsync();

                using JsonDocument weatherDoc =
                    JsonDocument.Parse(weatherJson);

                JsonElement current =
                    weatherDoc.RootElement.GetProperty("current");

                double temperature =
                    current.GetProperty("temperature_2m").GetDouble();

                double feelsLike =
                    current.GetProperty("apparent_temperature").GetDouble();

                int humidity =
                    current.GetProperty("relative_humidity_2m").GetInt32();

                int weatherCode =
                    current.GetProperty("weather_code").GetInt32();

                double windSpeed =
                    current.GetProperty("wind_speed_10m").GetDouble();

                string condition = weatherCode switch
                {
                    0 => "Aciq hava",
                    1 => "Esasen aciq",
                    2 => "Qismen buludlu",
                    3 => "Buludlu",
                    45 or 48 => "Dumanli",
                    51 or 53 or 55 => "Yungul ciskin",
                    61 or 63 or 65 => "Yagisli",
                    71 or 73 or 75 => "Qarli",
                    80 or 81 or 82 => "Leysan",
                    95 or 96 or 99 => "Gurultulu ve ildirimli",
                    _ => "Diger hava seraiti"
                };

                Console.WriteLine("\n--- Hava melumatlari ---");
                Console.WriteLine($"Seher: {cityName}");
                Console.WriteLine($"Temperatur: {temperature} °C");
                Console.WriteLine($"Hiss edilen: {feelsLike} °C");
                Console.WriteLine($"Hava seraiti: {condition}");
                Console.WriteLine($"Rutubet: {humidity}%");
                Console.WriteLine($"Kulek: {windSpeed} km/saat");
                Console.WriteLine("------------------------\n");
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"HTTP xetasi: {ex.Message}");
            }
            catch (JsonException)
            {
                Console.WriteLine("JSON melumatlarini oxumaq mumkun olmadi.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Xeta: {ex.Message}");
            }
        }

        Console.WriteLine("Proqram bitdi.");
    }
}
