using SAM.Picker;
using Xunit;

public class GetGameImageUrlTests
{
    [Fact]
    public void ReturnsAkamaiHeaderWhenAppDataMissing()
    {
        uint id = 123;
        string language = "english";

        string result = GameImageUrlResolver.GetGameImageUrl((a, b) => null, id, language);
        if (result == null)
        {
            result = $"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{id}/header.jpg";
        }

        Assert.Equal($"https://shared.akamai.steamstatic.com/store_item_assets/steam/apps/{id}/header.jpg", result);
    }
}
