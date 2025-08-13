using System;
using System.IO;
using System.Net;
using SAM.Picker;
using Xunit;

public class GameListTests
{
    [Fact]
    public void UsesLocalFileWhenNetworkFails()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(temp);
        string local = Path.Combine(temp, "games.xml");
        File.WriteAllText(local, "<games><game type='normal'>1</game></games>");

        byte[] bytes = GameList.Load(temp, _ => throw new WebException("fail"), out bool usedLocal);

        Assert.True(usedLocal);
        Assert.NotNull(bytes);
    }

    [Fact]
    public void ThrowsWhenAllSourcesFail()
    {
        string temp = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        // no directory created, so no local file
        Assert.Throws<InvalidOperationException>(() =>
            GameList.Load(temp, _ => throw new WebException("fail"), out _));
    }
}
