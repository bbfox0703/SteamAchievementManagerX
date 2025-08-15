using System;
using System.Drawing;
using System.IO;
using Xunit;

public class ImageValidationTests
{
    [Fact]
    public void CorruptedImageIsRejected()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }
        byte[] data = new byte[] { 0x00, 0x01, 0x02, 0x03 };
        using var stream = new MemoryStream(data);

        Bitmap? bitmap = null;
        try
        {
            using var image = Image.FromStream(stream, useEmbeddedColorManagement: false, validateImageData: true);
            bitmap = new Bitmap(image);
        }
        catch (ArgumentException)
        {
            // expected
        }
        catch (OutOfMemoryException)
        {
            // expected
        }

        Assert.Null(bitmap);
    }
}
