using System;
using System.Drawing;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OLED_Customizer.Services;
using OLED_Customizer.Utils;

namespace OLED_Customizer.Core
{
    public class DisplayManager
    {
        private readonly ILogger<DisplayManager> _logger;
        private readonly AppConfig _config;
        private readonly SteelSeriesAPI _steelSeries;
        private readonly HardwareMonitorService _hwMonitor;
        private readonly MediaService _mediaService;
        
        private readonly ClockRenderer _clockRenderer;
        private readonly Utils.TextRenderer _textRenderer;
        
        private bool _running;
        private Task? _loopTask;

        // State
        private DateTime _lastMediaActionTime = DateTime.MinValue;
        private string _lastTitle = "";
        private int _scrollOffset = 0;
        
        public DisplayManager(
            ILogger<DisplayManager> logger,
            SteelSeriesAPI steelSeries,
            HardwareMonitorService hwMonitor,
            MediaService mediaService)
        {
            _logger = logger;
            _steelSeries = steelSeries;
            _hwMonitor = hwMonitor;
            _mediaService = mediaService;
            
            _config = new AppConfig(); // TODO: Inject or Load
            _clockRenderer = new ClockRenderer();
            _textRenderer = new Utils.TextRenderer(fontSize: 12);
        }

        public void Start()
        {
            _running = true;
            _loopTask = Task.Run(LoopAsync);
        }

        public void Stop()
        {
            _running = false;
            _loopTask?.Wait(1000);
        }

        private async Task LoopAsync()
        {
            _logger.LogInformation("Display Manager setup...");
            await _steelSeries.InitializeAsync();
            await _mediaService.InitializeAsync();

            while (_running)
            {
                try
                {
                    Bitmap? frame = null;

                    // 1. Hardware Monitor (Priority)
                    if (_config.DisplayHwMonitor) 
                    {
                        // TODO: Render HW stats
                        // frame = RenderHwStats();
                    }

                    // 2. Media
                    if (frame == null && _config.DisplayPlayer)
                    {
                        var media = await _mediaService.GetCurrentMediaInfoAsync();
                        if (media != null && !media.Paused)
                        {
                            _lastMediaActionTime = DateTime.Now;
                            frame = RenderMedia(media);
                        }
                        else if (media != null && media.Paused && (DateTime.Now - _lastMediaActionTime).TotalSeconds < 5)
                        {
                            // Keep showing for a few seconds after pause
                            frame = RenderMedia(media); 
                        }
                    }

                    // 3. Clock (Default)
                    if (frame == null && _config.DisplayClock)
                    {
                        frame = _clockRenderer.Render(DateTime.Now);
                    }

                    if (frame != null)
                    {
                        var data = ImageUtils.ToSteelSeriesFormat(frame);
                        await _steelSeries.SendFrameAsync(data);
                        frame.Dispose();
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Render loop error");
                }

                await Task.Delay(1000 / _config.Fps);
            }
        }

        private Bitmap RenderMedia(MediaInfo media)
        {
            // Simple scrolling text for now
            string text = $"{media.Artist} - {media.Title}   ";
            
            // Scroll logic
            if (text != _lastTitle)
            {
                _scrollOffset = 0;
                _lastTitle = text;
            }
            else
            {
                _scrollOffset += 2; // rough scrolling speed
                int width = _textRenderer.MeasureWidth(text);
                if (_scrollOffset > width) _scrollOffset = -128; // loop
            }

            return _textRenderer.RenderText(text, 128, 40, _scrollOffset);
        }
    }
}
