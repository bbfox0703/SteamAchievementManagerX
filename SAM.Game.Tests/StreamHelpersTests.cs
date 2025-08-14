using System;
using System.IO;
using System.Text;
using SAM.Game;
using Xunit;

public class StreamHelpersTests
{
    [Fact]
    public void ReadValueS32_ThrowsOnTruncatedStream()
    {
        using var stream = new MemoryStream(new byte[3]);
        Assert.Throws<EndOfStreamException>(() => stream.ReadValueS32());
    }

    [Fact]
    public void ReadValueU32_ThrowsOnTruncatedStream()
    {
        using var stream = new MemoryStream(new byte[3]);
        Assert.Throws<EndOfStreamException>(() => stream.ReadValueU32());
    }

    [Fact]
    public void ReadValueU64_ThrowsOnTruncatedStream()
    {
        using var stream = new MemoryStream(new byte[7]);
        Assert.Throws<EndOfStreamException>(() => stream.ReadValueU64());
    }

    [Fact]
    public void ReadValueF32_ThrowsOnTruncatedStream()
    {
        using var stream = new MemoryStream(new byte[3]);
        Assert.Throws<EndOfStreamException>(() => stream.ReadValueF32());
    }

    [Fact]
    public void ReadStringAscii_ThrowsOnTruncatedStream()
    {
        using var stream = new MemoryStream(Encoding.ASCII.GetBytes("abc"));
        Assert.Throws<EndOfStreamException>(() => stream.ReadStringAscii());
    }
}
