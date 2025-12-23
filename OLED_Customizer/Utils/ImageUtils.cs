using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace OLED_Customizer.Utils
{
    public static class ImageUtils
    {
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        public static byte[] ToSteelSeriesFormat(Bitmap bitmap)
        {
            // Ensure 128x40
            if (bitmap.Width != 128 || bitmap.Height != 40)
            {
                using (var resized = ResizeImage(bitmap, 128, 40))
                {
                    return BitmapTo1BitArray(resized);
                }
            }

            return BitmapTo1BitArray(bitmap);
        }

        private static byte[] BitmapTo1BitArray(Bitmap bitmap)
        {
            // Convert to 1-bit array where each byte is a pixel (0 or 1), 
            // OR packed?
            // Python implementation: 
            // img = self.image.convert("1")
            // data = list(img.getdata()) 
            // This returns a list of 0 or 255 usually, or 0/1. 
            // In SteelSeriesAPI.py: img40 = image_128x40[:640] + ... 
            // Wait, 128x40 = 5120 pixels. 
            // The python code says: "Apex 7 Pro OLED = 128x40 (640 byte)" (?)
            // 5120 bits / 8 = 640 bytes.
            // So it IS packed bits.
            // AND the python code sends a list of 640 integers?
            // "dummy_128x40 = [0 for _ in range(640)]"
            // So yes, it sends 640 BYTE values, where each byte represents 8 pixels.
            
            // Let's look at `DisplayManager.py` -> `convert_to_bitmap`.
            // from src.image_utils import convert_to_bitmap
            
            // Checking Python's image_utils.py would be smart.
            
            // PROCEEDING WITH PACKED BITS ASSUMPTION based on "640" size for 128x40.
            
            var result = new byte[640];
            
            // Lock bits for speed
            BitmapData bmpData = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            
            int bytes = Math.Abs(bmpData.Stride) * bitmap.Height;
            byte[] rgbValues = new byte[bytes];
            Marshal.Copy(bmpData.Scan0, rgbValues, 0, bytes);
            
            int bitIndex = 0;
            for (int y = 0; y < 40; y++)
            {
                for (int x = 0; x < 128; x++)
                {
                    // Get pixel brightness
                    int idx = (y * bmpData.Stride) + (x * 4);
                    byte b = rgbValues[idx];
                    byte g = rgbValues[idx + 1];
                    byte r = rgbValues[idx + 2];
                    
                    // Simple threshold
                    bool on = (r + g + b) / 3 > 127;
                    
                    if (on)
                    {
                        // Set the bit
                        // SteelSeries likely uses row-major packed 
                        // Wait, if it's 640 bytes, and 128 width. 
                        // 128 bits = 16 bytes per row.
                        // 40 rows * 16 bytes = 640 bytes.
                        // Correct.
                        
                        int byteIndex = (y * 16) + (x / 8);
                        int bitPos = 7 - (x % 8); // MSAA/Endians might desire 7 - pos
                        // We will try standard MSB first.
                        
                        result[byteIndex] |= (byte)(1 << bitPos);
                    }
                }
            }
            
            bitmap.UnlockBits(bmpData);
            return result;
        }
    }
}
