using PrjProje.Application.Common;

namespace PrjProje.Tests;

public class YouTubeIdTests
{
    [Theory]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtube.com/watch?feature=share&v=dQw4w9WgXcQ&t=42")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ?si=abc")]
    [InlineData("https://www.youtube.com/shorts/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/embed/dQw4w9WgXcQ")]
    [InlineData("https://m.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("dQw4w9WgXcQ")]
    [InlineData("  https://youtu.be/dQw4w9WgXcQ  ")]
    public void Every_common_link_form_yields_the_id(string input) => Assert.Equal("dQw4w9WgXcQ", YouTubeId.Parse(input));

    [Theory]
    [InlineData("https://vimeo.com/123456")]
    [InlineData("https://www.youtube.com/channel/UCabc")]
    [InlineData("https://evil.example/watch?v=dQw4w9WgXcQ")]
    [InlineData("not a link")]
    [InlineData("")]
    [InlineData(null)]
    public void Anything_else_is_rejected(string? input) => Assert.Null(YouTubeId.Parse(input));
}
