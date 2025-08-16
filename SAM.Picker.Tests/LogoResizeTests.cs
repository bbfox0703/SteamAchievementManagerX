using System;
using System.Drawing;
using System.Runtime.Versioning;
using SAM.Picker;
using Xunit;

public class LogoResizeTests
{
    [SupportedOSPlatform("windows")]
    [Fact]
    public void OversizedLogoIsResizedToExpectedSize()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        using var source = new Bitmap(400, 400);
        using var resized = source.ResizeToFit(new Size(184, 69));

        Assert.Equal(184, resized.Width);
        Assert.Equal(69, resized.Height);
    }
}
