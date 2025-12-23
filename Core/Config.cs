using System.Text.Json;
using System.IO;

namespace OLED_Customizer.Core
{
    public class AppConfig
    {
        public int Fps { get; set; } = 10;
        public int SpotifyFetchDelay { get; set; } = 250; // ms
        public bool RgbEnabled { get; set; } = true;
        public int[] RgbColor { get; set; } = [255, 255, 255];
        
        // Display preferences
        public bool DisplayClock { get; set; } = true;
        public bool DisplayPlayer { get; set; } = true;
        public bool DisplayHwMonitor { get; set; } = false;

        // General
        public string ClockStyle { get; set; } = "Standard";
        public bool DisplaySeconds { get; set; } = true;
        public bool UseTurkishDays { get; set; } = false;

        // Spotify
        public string SpotifyClientId { get; set; } = "";
        public string SpotifyClientSecret { get; set; } = "";
        public string SpotifyRedirectUri { get; set; } = "http://localhost:8888/callback";
        public int LocalPort { get; set; } = 2408;

        // Advanced
        public int ScrollbarPadding { get; set; } = 2;
        public int TextPaddingLeft { get; set; } = 30;

        // Keys (Placeholder)
        public string HotkeyMonitor { get; set; } = "";
        public string HotkeyMute { get; set; } = "";

        public static AppConfig Load(string path)
        {
            if (!File.Exists(path))
            {
                var defaults = new AppConfig();
                Save(path, defaults);
                return defaults;
            }

            try
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
            catch
            {
                return new AppConfig();
            }
        }

        public static void Save(string path, AppConfig config)
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            var json = JsonSerializer.Serialize(config, options);
            File.WriteAllText(path, json);
        }

        public void SavePreferences()
        {
            Save("config.json", this);
        }
    }
}
