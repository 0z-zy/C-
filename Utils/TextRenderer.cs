using System;
using System.Drawing;
using System.Drawing.Text;

namespace OLED_Customizer.Utils
{
    public class TextRenderer
    {
        private readonly Font _font;
        private readonly Brush _brush;

        public TextRenderer(string fontFamily = "Arial", float fontSize = 10, FontStyle style = FontStyle.Regular)
        {
            try 
            {
                _font = new Font(fontFamily, fontSize, style);
            }
            catch
            {
                _font = new Font(SystemFonts.DefaultFont.FontFamily, fontSize, style);
            }
            _brush = Brushes.White;
        }

        public Bitmap RenderText(string text, int width, int height, int scrollOffset = 0)
        {
            var bmp = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bmp))
            {
                g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit; // Sharp text for OLED
                g.Clear(Color.Black);
                
                // Measure string to vertically center
                var size = g.MeasureString(text, _font);
                float y = (height - size.Height) / 2;
                
                g.DrawString(text, _font, _brush, -scrollOffset, y);
            }
            return bmp;
        }

        public int MeasureWidth(string text)
        {
            // Dummy bitmap for measurement
            using (var bmp = new Bitmap(1, 1))
            using (var g = Graphics.FromImage(bmp))
            {
                return (int)g.MeasureString(text, _font).Width;
            }
        }
    }
}
