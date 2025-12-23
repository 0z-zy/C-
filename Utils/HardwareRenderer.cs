using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using OLED_Customizer.Core;

namespace OLED_Customizer.Utils
{
    public class HardwareRenderer : IDisposable
    {
        private readonly PrivateFontCollection _pfc;
        private readonly Font _font;
        private readonly Bitmap _cpuIcon;
        private readonly Bitmap _gpuIcon;
        private readonly Bitmap _ramIcon;
        private readonly AppConfig _config; // Uses colors? Python config has primary/secondary colors.
        
        // Python config uses primary (white=1) and secondary (black=0) usually.
        private readonly Brush _brush = Brushes.White;

        public HardwareRenderer(AppConfig config)
        {
            _config = config;
            _pfc = new PrivateFontCollection();
            
            // Load Font
            string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts", "VerdanaBold.ttf");
            if (File.Exists(fontPath))
            {
                _pfc.AddFontFile(fontPath);
                _font = new Font(_pfc.Families[0], 11, FontStyle.Regular, GraphicsUnit.Pixel); // Python size 11
            }
            else
            {
                _font = new Font("Verdana", 11, FontStyle.Bold, GraphicsUnit.Pixel);
            }

            // Load Icons
            string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "icons");
            _cpuIcon = LoadIcon(Path.Combine(iconPath, "cpu_icon.png"));
            _gpuIcon = LoadIcon(Path.Combine(iconPath, "gpu_icon.png"));
            _ramIcon = LoadIcon(Path.Combine(iconPath, "ram_icon.png"));
        }

        private Bitmap LoadIcon(string path)
        {
            if (File.Exists(path))
            {
                try
                {
                    var bmp = new Bitmap(path);
                    return bmp;
                }
                catch { }
            }
            return new Bitmap(12, 12);
        }

        public Bitmap Render(float? cpuTemp, float? cpuLoad, float? gpuTemp, float? gpuLoad, float? ramUsed, float? ramTotal)
        {
            var bmp = new Bitmap(128, 40);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit; // Sharp pixel look

                int colWidth = 128 / 3;
                int c1_x = 0;
                int c2_x = colWidth;
                int c3_x = colWidth * 2;

                int y_icon = 0;
                int y_text1 = 13;
                int y_text2 = 26;

                // Column 1: CPU
                DrawCenteredIcon(g, _cpuIcon, c1_x, colWidth, y_icon);
                DrawCenteredText(g, cpuTemp.HasValue ? $"{Math.Round(cpuTemp.Value)}°" : "--", c1_x, colWidth, y_text1);
                DrawCenteredText(g, cpuLoad.HasValue ? $"{Math.Round(cpuLoad.Value)}%" : "0%", c1_x, colWidth, y_text2);

                // Column 2: GPU
                DrawCenteredIcon(g, _gpuIcon, c2_x, colWidth, y_icon);
                DrawCenteredText(g, gpuTemp.HasValue ? $"{Math.Round(gpuTemp.Value)}°" : "--", c2_x, colWidth, y_text1);
                DrawCenteredText(g, gpuLoad.HasValue ? $"{Math.Round(gpuLoad.Value)}%" : "0%", c2_x, colWidth, y_text2);

                // Column 3: RAM
                DrawCenteredIcon(g, _ramIcon, c3_x, colWidth, y_icon);
                DrawCenteredText(g, ramUsed.HasValue ? $"{ramUsed.Value:F1}G" : "--", c3_x, colWidth, y_text1);
                DrawCenteredText(g, ramTotal.HasValue ? $"{Math.Round(ramTotal.Value)}GB" : "16GB", c3_x, colWidth, y_text2); // Default fallback if total calc missing
            }
            return bmp;
        }

        private void DrawCenteredText(Graphics g, string text, int x, int width, int y)
        {
            var size = g.MeasureString(text, _font);
            // MeasureString adds some padding, TextRenderer is better usually but Graphics fits 1-bit style
            // Let's stick to simple centering
            float tx = x + (width - size.Width) / 2 + 2; // +2 fudge factor for GDI+ padding
            g.DrawString(text, _font, _brush, tx, y);
        }

        private void DrawCenteredIcon(Graphics g, Bitmap icon, int x, int width, int y)
        {
            if (icon == null) return;
            int ix = x + (width - icon.Width) / 2;
            g.DrawImage(icon, ix, y);
        }

        public void Dispose()
        {
            _pfc?.Dispose();
            _font?.Dispose();
            _cpuIcon?.Dispose();
            _gpuIcon?.Dispose();
            _ramIcon?.Dispose();
        }
    }
}
