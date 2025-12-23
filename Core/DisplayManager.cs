using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq; // For SequenceEqual
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
        
        // Renderers & Receivers
        private readonly ClockRenderer _clockRenderer;
        private readonly ExtensionReceiver _extensionReceiver;
        private readonly HardwareRenderer _hwRenderer;
        private readonly Utils.TextRenderer _textRenderer;

        private bool _running;
        private Task? _loopTask;

        // State
        private byte[]? _lastFrameData;
        private DateTime _lastMediaActionTime = DateTime.MinValue;
        private string _lastTitle = "";
        private int _scrollOffset = 0;
        
        private long _lastExtensionDataMs = 0;
        private const int EXTENSION_LOCK_MS = 5000;

        public DisplayManager(
            ILogger<DisplayManager> logger,
            AppConfig config,
            SteelSeriesAPI steelSeries,
            HardwareMonitorService hwMonitor,
            MediaService mediaService)
        {
            _logger = logger;
            _config = config;
            _steelSeries = steelSeries;
            _hwMonitor = hwMonitor;
            _mediaService = mediaService;
            
            _clockRenderer = new ClockRenderer();
            _textRenderer = new Utils.TextRenderer(fontSize: 10);
            _hwRenderer = new HardwareRenderer(_config);
            _extensionReceiver = new ExtensionReceiver(LoggerFactory.Create(b => b.AddConsole()).CreateLogger<ExtensionReceiver>());
        }

        public void Start()
        {
            if (_running) return;
            _running = true;
            _extensionReceiver.Start();
            _loopTask = Task.Run(LoopAsync);
        }

        public void Stop()
        {
            _running = false;
            _loopTask?.Wait(1000);
            _hwRenderer.Dispose();
        }

        private async Task LoopAsync()
        {
            _logger.LogInformation("Display Manager loop starting...");
            await _steelSeries.InitializeAsync();
            await _mediaService.InitializeAsync();

            while (_running)
            {
                try
                {
                    Bitmap? frame = null;

                    // 1. Hardware Monitor
                    if (_config.DisplayHwMonitor) 
                    {
                         frame = RenderHwStats();
                    }

                    // 2. Media (Extension > SMTC)
                    if (frame == null && _config.DisplayPlayer)
                    {
                        var (title, artist, progress, duration, isPlaying) = await GetMediaDataAsync();
                        
                        if (!string.IsNullOrEmpty(title))
                        {
                            if (isPlaying)
                            {
                                _lastMediaActionTime = DateTime.Now;
                                frame = RenderMedia(title, artist, progress, duration);
                            }
                            else if ((DateTime.Now - _lastMediaActionTime).TotalSeconds < 5)
                            {
                                // Show paused for 5 seconds
                                frame = RenderMedia(title, artist, progress, duration);
                            }
                        }
                    }

                    // 3. Clock
                    if (frame == null && _config.DisplayClock)
                    {
                        frame = _clockRenderer.Render(_config);
                    }

                    if (frame != null)
                    {
                        var data = ImageUtils.ToSteelSeriesFormat(frame);
                        
                        // Diffing
                        if (_lastFrameData == null || !data.SequenceEqual(_lastFrameData))
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
                    DebugLog($"Render Loop Checkpoint Error: {ex.Message} \nStack: {ex.StackTrace}");
                }

                int delay = 1000 / Math.Max(1, _config.Fps);
                await Task.Delay(delay);
            }
        }

        private void DebugLog(string msg)
        {
            try { System.IO.File.AppendAllText("debug.log", $"{DateTime.Now}: {msg}\n"); } catch {}
        }

        private long _lastMediaPollMs = 0;
        private (string, string, double, double, bool) _cachedMediaData = ("", "", 0, 0, false);

        private async Task<(string title, string artist, double progress, double duration, bool isPlaying)> GetMediaDataAsync()
        {
            try
            {
                long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                // A. Extension (Fast, always check)
                var extData = _extensionReceiver.GetLatestData();
                if (extData != null)
                {
                    _lastExtensionDataMs = now;
                    string t = extData.ContainsKey("title") ? extData["title"]?.ToString() ?? "" : "";
                    string a = extData.ContainsKey("artist") ? extData["artist"]?.ToString() ?? "" : "";
                    
                    double p = 0, d = 0;
                    if (extData.ContainsKey("progress")) double.TryParse(extData["progress"]?.ToString(), out p);
                    if (extData.ContainsKey("duration")) double.TryParse(extData["duration"]?.ToString(), out d);
                    
                    bool playing = false;
                    if (extData.ContainsKey("playing")) bool.TryParse(extData["playing"]?.ToString(), out playing);
                    
                    // Reset SMTC cache if extension is active
                    _cachedMediaData = (t, a, p, d, playing);
                    return _cachedMediaData;
                }

                // B. SMTC Fallback (Poll throttled)
                if (now - _lastExtensionDataMs > EXTENSION_LOCK_MS)
                {
                     // Only poll SMTC every 500ms to allow smooth rendering of other things
                     if (now - _lastMediaPollMs > 500)
                     {
                        _lastMediaPollMs = now;
                        var media = await _mediaService.GetCurrentMediaInfoAsync();
                        if (media != null)
                        {
                            _cachedMediaData = (media.Title, media.Artist, media.Position / 1000.0, media.Duration / 1000.0, !media.Paused);
                        }
                     }
                     
                     // If playing, simulate progress locally for smooth bar?
                     // For now just return cached
                     return _cachedMediaData;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Media Poll Error: {ex.Message}");
            }
            
            return ("", "", 0, 0, false);
        }

        private Bitmap RenderHwStats()
        {
            var (cpuTemp, cpuLoad, gpuTemp, gpuLoad, ramUsed, ramAvail) = _hwMonitor.GetStats();
            // Calculate total RAM for Python-like display (Used / Total)
            // LHM gives used/available. Total = Used + Available.
            float ramTotal = (ramUsed ?? 0) + (ramAvail ?? 0);
            if (ramTotal < 1) ramTotal = 32; // Fallback
            
            return _hwRenderer.Render(cpuTemp, cpuLoad, gpuTemp, gpuLoad, ramUsed, ramTotal);
        }
        
        private Bitmap RenderMedia(string title, string artist, double progressSec, double durationSec)
        {
            var bmp = new Bitmap(128, 40);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
                
                // Construct text
                string line1 = title;
                string line2 = artist;
                
                // Load font (use helper or simple default)
                // Python uses "VerdanaBold" 11px for stats, but what for media?
                // Probably smaller or same. Let's use generic Sans Serif 8ish.
                using var font = new Font("Verdana", 8); // System Font
                using var brush = new SolidBrush(Color.White);

                // Scrolling logic for Line 1 (Title)
                // Python logic: "text = artist - title". Scrolled.
                // But Python DisplayManager.py line 400: `player.update_song(...)`. 
                // `SpotifyPlayer.py` renders it. Let's check visual if needed.
                // Assuming standard 2-line or scrolling 1-line.
                // Let's implement scrolling 1-line for now as per previous C# impl but better.
                
                string fullText = $"{artist} - {title}";
                var size = g.MeasureString(fullText, font);
                
                if (fullText != _lastTitle)
                {
                    _scrollOffset = 0;
                    _lastTitle = fullText;
                }
                
                if (size.Width > 128)
                {
                    _scrollOffset -= 2; // scroll left
                    if (Math.Abs(_scrollOffset) > size.Width) _scrollOffset = 128;
                }
                else
                {
                    _scrollOffset = (128 - (int)size.Width) / 2; // center if fits
                }
                
                g.DrawString(fullText, font, brush, _scrollOffset, 0);

                // Progress Bar
                if (durationSec > 0)
                {
                    int barWidth = (int)((progressSec / durationSec) * 128);
                    g.FillRectangle(brush, 0, 35, barWidth, 5);
                }
                
                // Time Text (e.g. 1:23 / 3:45) - Python draws it?
            }
            return bmp;
        }
    }
}
