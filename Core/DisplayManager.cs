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
                         frame = RenderHwStats();
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
                        
                        // Only send if changed
                        if (_lastFrameData == null || !ArraysEqual(_lastFrameData, data)) // need to impl ArraysEqual or use SequenceEqual
                        {
                             await _steelSeries.SendFrameAsync(data);
                             _lastFrameData = data;
                        }
                        
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

        private byte[]? _lastFrameData;
        private bool ArraysEqual(byte[] a, byte[] b)
        {
            if (a.Length != b.Length) return false;
            for(int i=0; i<a.Length; i++) if(a[i]!=b[i]) return false;
            return true;
        }

        private Bitmap RenderHwStats()
        {
            // Simple text rendering for HW Stats
            var (cpuTemp, cpuLoad, gpuTemp, gpuLoad, ramLoad) = _hwMonitor.GetStats();
            
            var bmp = new Bitmap(128, 40);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;
                
                // Font for stats - small
                using (var font = new Font("Arial", 8))
                using (var brush = new SolidBrush(Color.White))
                {
                    string cpu = $"CPU: {(int)(cpuLoad ?? 0)}% {(int)(cpuTemp ?? 0)}C";
                    string gpu = $"GPU: {(int)(gpuLoad ?? 0)}% {(int)(gpuTemp ?? 0)}C";
                    string ram = $"RAM: {(int)(ramLoad ?? 0)}%";
                    
                    g.DrawString(cpu, font, brush, 0, 0);
                    g.DrawString(gpu, font, brush, 0, 12);
                    g.DrawString(ram, font, brush, 0, 24);
                }
            }
            return bmp;
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
