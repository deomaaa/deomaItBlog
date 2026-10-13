using DeomaBlog.Domain.Common;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.UnitTests.Domain;

public sealed class ReadingTimeTests
{
    [Fact]
    public void A_short_post_still_takes_at_least_one_minute()
        => Assert.Equal(1, ReadingTime.FromMarkdown("Just a sentence.").Minutes);

    [Fact]
    public void Word_count_is_divided_by_the_reading_speed()
    {
        var markdown = string.Join(' ', Enumerable.Repeat("word", 180 * 3));

        Assert.Equal(3, ReadingTime.FromMarkdown(markdown).Minutes);
    }

    [Fact]
    public void Code_blocks_do_not_inflate_the_estimate()
    {
        var prose = string.Join(' ', Enumerable.Repeat("word", 180));
        var markdown = $"{prose}\n\n```csharp\n{string.Join(' ', Enumerable.Repeat("token", 5000))}\n```";

        Assert.Equal(1, ReadingTime.FromMarkdown(markdown).Minutes);
    }

    [Fact]
    public void Display_is_human_readable()
    {
        Assert.Equal("1 min read", ReadingTime.FromMinutes(1).Display);
        Assert.Equal("7 min read", ReadingTime.FromMinutes(7).Display);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(601)]
    public void FromMinutes_rejects_values_outside_the_sane_range(int minutes)
        => Assert.Throws<DomainException>(() => ReadingTime.FromMinutes(minutes));
}
