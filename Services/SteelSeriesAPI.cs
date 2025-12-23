using System;
using System.IO;
using System.Net.Http;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace OLED_Customizer.Services
{
    public class SteelSeriesAPI
    {
        private readonly ILogger<SteelSeriesAPI> _logger;
        private readonly HttpClient _httpClient;
        private string _address = "";
        private const string GAME = "OLED_CUSTOMIZER_V3";
        private const string GAMESENSE_DISPLAY_NAME = "OLED Customizer";
        private const string AUTHOR = "0z-zy"; // Original Author
        private const string EVENT = "UPDATE";

        public SteelSeriesAPI(ILogger<SteelSeriesAPI> logger)
        {
            _logger = logger;
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromMilliseconds(500); // Fail fast
        }

        public async Task InitializeAsync()
        {
            await RetrieveAddressAsync();
        }

        private async Task RetrieveAddressAsync()
        {
            var programData = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData);
            var corePropsPaths = new[]
            {
                Path.Combine(programData, "SteelSeries", "SteelSeries Engine 3", "coreProps.json"),
                Path.Combine(programData, "SteelSeries", "GG", "coreProps.json")
            };

            string? corePropsFile = null;
            foreach (var p in corePropsPaths)
            {
                if (File.Exists(p))
                {
                    corePropsFile = p;
                    break;
                }
            }

            if (corePropsFile == null)
            {
                _logger.LogError("coreProps.json not found (Engine/GG not running?)");
                return;
            }

            try
            {
                var json = await File.ReadAllTextAsync(corePropsFile);
                using var doc = JsonDocument.Parse(json);
                var address = doc.RootElement.GetProperty("address").GetString();
                _address = "http://" + address;
                
                _logger.LogInformation($"Found local address API : {_address}");

                await RemoveGameAsync();
                await RegisterGameAsync();
                await BindGameEventAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Could not connect/register to SteelSeries GameSense API");
            }
        }

        private async Task PostAsync(string endpoint, object data)
        {
            if (string.IsNullOrEmpty(_address)) return;

            try
            {
                var json = JsonSerializer.Serialize(data);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync(_address + endpoint, content);
                
                if (!response.IsSuccessStatusCode)
                {
                    // _logger.LogWarning($"API error {endpoint}: {response.StatusCode}");
                }
            }
            catch
            {
                // Ignore connection errors during loop
            }
        }

        public async Task RegisterGameAsync()
        {
            var data = new Dictionary<string, object>
            {
                { "game", GAME },
                { "game_display_name", GAMESENSE_DISPLAY_NAME },
                { "developer", AUTHOR },
                { "deinitialize_timer_length_ms", 60000 }
            };
            await PostAsync("/game_metadata", data);
        }

        public async Task RemoveGameAsync()
        {
            await PostAsync("/remove_game", new { game = GAME });
        }

        public async Task BindGameEventAsync()
        {
            var dummy128x40 = new byte[640];
            
            var handlers = new List<object>
            {
                new Dictionary<string, object>
                {
                    { "device-type", "screened-128x40" },
                    { "mode", "screen" },
                    { "zone", "one" },
                    { "datas", new[] 
                        {
                            new Dictionary<string, object> 
                            { 
                                { "has-text", false }, 
                                { "image-data", dummy128x40 } 
                            }
                        } 
                    }
                }
            };

            var data = new
            {
                game = GAME,
                @event = EVENT,
                min_value = 0,
                max_value = 1,
                icon_id = 1,
                handlers = handlers
            };
            
            await PostAsync("/bind_game_event", data);
            _logger.LogInformation("Binding game event (128x40 only)");
        }

        public async Task SendFrameAsync(byte[] imageData)
        {
            if (imageData.Length != 640)
            {
               // Just truncate or pad if needed
            }
            
            var frameData = new int[imageData.Length];
            for(int i=0; i<imageData.Length; i++) frameData[i] = imageData[i];

            var data = new
            {
                game = GAME,
                @event = EVENT,
                data = new
                {
                    frame = new Dictionary<string, object>
                    {
                        { "image-data-128x40", frameData }
                    }
                }
            };

            await PostAsync("/game_event", data);
        }
    }
}
