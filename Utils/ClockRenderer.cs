using System;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using OLED_Customizer.Core;

namespace OLED_Customizer.Utils
{
    public class ClockRenderer : IDisposable
    {
        private readonly PrivateFontCollection _pfc;
        private readonly Font _fontDigiBig;
        private readonly Font _fontDigiMed;
        private readonly Font _fontDigiSmall;
        private readonly Font _fontHuge;
        private readonly Brush _brush;

        public ClockRenderer()
        {
            _pfc = new PrivateFontCollection();
            _brush = Brushes.White;

            // Load Fonts
            string fontPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "fonts", "DS-DIGIB.ttf");
            if (File.Exists(fontPath))
            {
                _pfc.AddFontFile(fontPath);
                var family = _pfc.Families[0];
                // Reduced sizes to fit 128x40 better
                _fontDigiBig = new Font(family, 22, FontStyle.Regular, GraphicsUnit.Pixel);
                _fontDigiMed = new Font(family, 18, FontStyle.Regular, GraphicsUnit.Pixel);
                _fontDigiSmall = new Font(family, 12, FontStyle.Regular, GraphicsUnit.Pixel);
                _fontHuge = new Font(family, 32, FontStyle.Regular, GraphicsUnit.Pixel);
            }
            else
            {
                // Fallback
                _fontDigiBig = new Font("Arial", 22, FontStyle.Bold, GraphicsUnit.Pixel);
                _fontDigiMed = new Font("Arial", 18, FontStyle.Bold, GraphicsUnit.Pixel);
                _fontDigiSmall = new Font("Arial", 12, FontStyle.Regular, GraphicsUnit.Pixel);
                _fontHuge = new Font("Arial", 32, FontStyle.Bold, GraphicsUnit.Pixel);
            }
        }

        public Bitmap Render(AppConfig config)
        {
            var bmp = new Bitmap(128, 40);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Black);
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;

                DateTime now = DateTime.Now;
                string style = config.ClockStyle ?? "Standard";
                
                // Prepare Text
                (string timeText, string dateText) = GetTimeStrings(now, config);

                // Draw based on Style
                StringFormat centerFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center
                };
                
                int cx = 128 / 2;
                int cy = 40 / 2;

                if (style == "Big Timer")
                {
                     g.DrawString(timeText, _fontHuge, _brush, cx, cy + 5, centerFormat); // +5 fudge
                }
                else if (style == "Date Focused")
                {
                     g.DrawString(dateText, _fontDigiMed, _brush, cx, cy - 8, centerFormat);
                     g.DrawString(timeText, _fontDigiSmall, _brush, cx, cy + 12, centerFormat);
                }
                else // Standard
                {
                     g.DrawString(timeText, _fontDigiBig, _brush, cx, cy - 6, centerFormat);
                     g.DrawString(dateText, _fontDigiSmall, _brush, cx, cy + 10, centerFormat);
                }
            }
            return bmp;
        }

        private (string time, string date) GetTimeStrings(DateTime now, AppConfig config)
        {
            string seconds = config.DisplaySeconds ? ":ss" : "";
            // Assuming 24h default for now unless we add 12h option to Config (it was missing in Config.cs but Python has it)
            // Python: self.date_format == 12
            // Config.cs I didn't see DateFormat property, only UseTurkishDays/DisplaySeconds.
            // I'll default to 24h as per simple port unless user adds it.
            
            string timeText = now.ToString("HH:mm" + seconds);
            
            // Days
            string dayStr = now.ToString("ddd");
            if (config.UseTurkishDays)
            {
                dayStr = dayStr switch
                {
                    "Mon" => "Pzt", "Tue" => "Sal", "Wed" => "Çar", "Thu" => "Per", "Fri" => "Cum", "Sat" => "Cmt", "Sun" => "Paz",
                    _ => dayStr
                };
            }
            
            string dateText = $"{dayStr} {now:dd/MM/yyyy}";
            
            return (timeText, dateText);
        }

        public void Dispose()
        {
            _pfc?.Dispose();
            _fontDigiBig?.Dispose();
            _fontDigiMed?.Dispose();
            _fontDigiSmall?.Dispose();
            _fontHuge?.Dispose();
        }
    }
}
