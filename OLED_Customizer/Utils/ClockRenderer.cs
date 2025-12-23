using System;
using System.Drawing;
using System.Drawing.Text;

namespace OLED_Customizer.Utils
{
    public class ClockRenderer
    {
        private readonly Font _bigFont;
        private readonly Font _smallFont;
        private readonly Brush _brush;

        public ClockRenderer()
        {
            // Try to find a nice font, otherwise fallback
            string family = "Arial"; // Ideally something like "Impact" or "Segoe UI"
            _bigFont = new Font(family, 24, FontStyle.Bold);
            _smallFont = new Font(family, 10, FontStyle.Regular);
            _brush = Brushes.White;
        }

        public Bitmap Render(DateTime now)
        {
            var bmp = new Bitmap(128, 40);
            using (var g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = TextRenderingHint.AntiAliasGridFit; 
                g.Clear(Color.Black);

                string timeStr = now.ToString("HH:mm");
                string dateStr = now.ToString("dd MMM yyyy"); // TODO: Localization

                // Draw Time centered-ish
                var timeSize = g.MeasureString(timeStr, _bigFont);
                // g.DrawString(timeStr, _bigFont, _brush, (128 - timeSize.Width) / 2, -5); 
                // Left align logic from original? Original centers it.
                
                // Let's do a simple layout: Time on top/middle, Date small below or right?
                // Original Timer.py has different styles. Let's do a simple "Large Time" style default.
                
                g.DrawString(timeStr, _bigFont, _brush, 10, -2);
                g.DrawString(now.ToString(":ss"), _smallFont, _brush, 10 + timeSize.Width - 10, 12);
                
                g.DrawString(dateStr, _smallFont, _brush, 10, 25);
            }
            return bmp;
        }
    }
}
