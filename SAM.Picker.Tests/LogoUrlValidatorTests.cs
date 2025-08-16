using SAM.Picker;
using Xunit;

public class LogoUrlValidatorTests
{
    [Theory]
    [InlineData("https://shared.cloudflare.steamstatic.com/store_item_assets/steam/apps/123/foo.png", true)]
    [InlineData("https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/123/foo.png", true)]
    [InlineData("https://cdn.steamstatic.com/steamcommunity/public/images/apps/123/foo.jpg", true)]
    [InlineData("http://cdn.steamstatic.com/steamcommunity/public/images/apps/123/foo.jpg", false)]
    [InlineData("https://example.com/image.png", false)]
    [InlineData("not a url", false)]
    public void ValidatesUrls(string url, bool expected)
    {
        var result = ImageUrlValidator.TryCreateUri(url, out var uri);
        Assert.Equal(expected, result);
        if (expected)
        {
            Assert.NotNull(uri);
        }
        else
        {
            Assert.Null(uri);
        }
    }
}

