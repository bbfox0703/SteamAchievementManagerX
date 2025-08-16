using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.Versioning;

namespace SAM.Picker
{
    [SupportedOSPlatform("windows")]
    internal static class BitmapExtensions
    {
        public static Bitmap ResizeToFit(this Image image, Size target)
        {
            var scale = Math.Min((float)target.Width / image.Width, (float)target.Height / image.Height);
            var width = (int)Math.Round(image.Width * scale);
            var height = (int)Math.Round(image.Height * scale);

            Bitmap bitmap = new(target.Width, target.Height);
            using var g = Graphics.FromImage(bitmap);
            g.Clear(Color.Transparent);
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.DrawImage(image, (target.Width - width) / 2, (target.Height - height) / 2, width, height);
            return bitmap;
        }

        public static byte[] ToPngBytes(this Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Png);
            return ms.ToArray();
        }
    }
}
