using PrjProje.Application.Common;

namespace PrjProje.Tests;

public class WhatsAppLinkTests
{
    [Fact]
    public void Turkish_mobile_with_message_builds_wa_me_link() =>
        Assert.Equal("https://wa.me/905317960029?text=B%20400%20hakk%C4%B1nda%20bilgi", WhatsAppLink.Build("0 531 796 00 29", "B 400 hakkında bilgi"));

    [Theory]
    [InlineData("905317960029")]
    [InlineData("+90 531 796 00 29")]
    [InlineData("0090 531 796 00 29")]
    [InlineData("531 796 00 29")]
    public void Number_formats_normalize_to_the_same_link(string number) =>
        Assert.Equal("https://wa.me/905317960029", WhatsAppLink.Build(number, null));

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" - ")]
    public void Missing_number_gives_no_link(string? number) => Assert.Null(WhatsAppLink.Build(number, "Merhaba"));
}
