using System;
using System.Drawing;
using System.IO;
using Xunit;

public class ImageValidationTests
{
    [Fact]
    public void CorruptedLogoIsRejected()
    {
        byte[] data = new byte[] { 0x10, 0x20, 0x30, 0x40 };
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
