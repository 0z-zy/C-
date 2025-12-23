using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using OLED_Customizer.Core;

namespace OLED_Customizer.Utils
{
    public class VolumeRenderer : IDisposable
    {
        private readonly Dictionary<string, Bitmap> _icons = new Dictionary<string, Bitmap>();
        
        // Config colors simulation (white primary, black secondary)
        private readonly Brush _brush = Brushes.White;
        private readonly Pen _pen = Pens.White;

        public VolumeRenderer()
        {
            LoadIcons();
        }

        private void LoadIcons()
        {
            var mapping = new Dictionary<string, string>
            {
                { "speaker_mute", "speaker_mute.png" },
                { "speaker_low", "speaker_low.png" },
                { "speaker_mid", "speaker_mid.png" },
                { "speaker_high", "speaker_high.png" },
                { "mic_on", "mic_on.png" },
                { "mic_off", "mic_off.png" }
            };

            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icons");
            
            foreach (var kvp in mapping)
            {
                string path = Path.Combine(iconPath, kvp.Value);
                if (File.Exists(path))
                {
                    try
                    {
                        _icons[kvp.Key] = new Bitmap(path);
                    }
                    catch { }
                }
            }
        }

        public Bitmap Render(float volume, bool isMute, bool isMicMute)
        {
            var bmp = new Bitmap(128, 40);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;

                // 1. Speaker Icon
                string iconKey = "speaker_mute";
                if (!isMute)
                {
                    if (volume <= 0) iconKey = "speaker_mute";
                    else if (volume < 33) iconKey = "speaker_low";
                    else if (volume < 66) iconKey = "speaker_mid";
                    else iconKey = "speaker_high";
                }

                if (_icons.ContainsKey(iconKey))
                {
                    g.DrawImage(_icons[iconKey], 2, 14);
                }

                // 2. Mic Icon
                string micKey = isMicMute ? "mic_off" : "mic_on";
                if (_icons.ContainsKey(micKey))
                {
                    g.DrawImage(_icons[micKey], 128 - 14, 14);
                }

                // 3. Bar
                int barX1 = 18;
                int barX2 = 128 - 18; // Always assume mic present/width
                int barY1 = 16;
                int barY2 = 40 - 16; // height is 40, so y2 = 24. Height 8px.

                // Draw outline (Rectangle excluding end point usually, GDI+ includes? Rectangle(x,y,w,h))
                // Python: (x1, y1, x2, y2) -> GDI+ (x, y, w, h)
                // w = x2 - x1
                // h = y2 - y1
                g.DrawRectangle(_pen, barX1, barY1, barX2 - barX1, barY2 - barY1);

                if (!isMute && volume > 0)
                {
                    int maxWidth = (barX2 - barX1) - 4; // -2 padding each side approx
                    int fillWidth = (int)(maxWidth * (volume / 100.0f));
                    
                    if (fillWidth > 0)
                    {
                        g.FillRectangle(_brush, barX1 + 2, barY1 + 2, fillWidth, (barY2 - barY1) - 3);
                    }
                }
            }
            return bmp;
        }

        public void Dispose()
        {
            foreach (var icon in _icons.Values) icon.Dispose();
            _icons.Clear();
        }
    }
}
