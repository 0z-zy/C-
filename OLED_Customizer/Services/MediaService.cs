using System;
using System.Threading.Tasks;
using Windows.Media.Control;
using Microsoft.Extensions.Logging;

namespace OLED_Customizer.Services
{
    public class MediaService
    {
        private readonly ILogger<MediaService> _logger;
        private GlobalSystemMediaTransportControlsSessionManager? _manager;
        
        public MediaService(ILogger<MediaService> logger)
        {
            _logger = logger;
        }

        public async Task InitializeAsync()
        {
            try
            {
                _manager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to initialize SMTC Manager");
            }
        }

        public async Task<MediaInfo?> GetCurrentMediaInfoAsync()
        {
            if (_manager == null) return null;

            try
            {
                var session = _manager.GetCurrentSession();
                if (session == null) return null;

                var props = await session.TryGetMediaPropertiesAsync();
                if (props == null) return null;

                var timeline = session.GetTimelineProperties();
                
                string sourceApp = session.SourceAppUserModelId; // e.g. Spotify.exe or Chrome

                return new MediaInfo
                {
                    Title = props.Title,
                    Artist = props.Artist,
                    Source = sourceApp,
                    Paused = session.GetPlaybackInfo()?.PlaybackStatus != GlobalSystemMediaTransportControlsSessionPlaybackStatus.Playing,
                    Position = timeline?.Position.TotalMilliseconds ?? 0,
                    Duration = timeline?.EndTime.TotalMilliseconds ?? 0
                };
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class MediaInfo
    {
        public string Title { get; set; } = "";
        public string Artist { get; set; } = "";
        public string Source { get; set; } = "";
        public bool Paused { get; set; }
        public double Position { get; set; }
        public double Duration { get; set; }
    }
}
