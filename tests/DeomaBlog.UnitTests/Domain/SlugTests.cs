using DeomaBlog.Domain.Common;
using DeomaBlog.Domain.Posts;

namespace DeomaBlog.UnitTests.Domain;

public sealed class SlugTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("  IEnumerable vs IQueryable  ", "ienumerable-vs-iqueryable")]
    [InlineData("C#: records & classes", "c-records-classes")]
    [InlineData("Multiple   spaces---and__underscores", "multiple-spaces-and-underscores")]
    public void FromTitle_normalizes_a_title_into_a_url_friendly_slug(string title, string expected)
        => Assert.Equal(expected, Slug.FromTitle(title).Value);

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("!!!")]
    public void FromTitle_rejects_titles_without_usable_characters(string title)
        => Assert.Throws<DomainException>(() => Slug.FromTitle(title));

    [Theory]
    [InlineData("UPPER")]
    [InlineData("has space")]
    [InlineData("double--dash")]
    [InlineData("-leading-dash")]
    [InlineData("trailing-dash-")]
    public void Create_rejects_values_that_do_not_match_the_slug_pattern(string value)
        => Assert.Throws<DomainException>(() => Slug.Create(value));

    [Fact]
    public void Two_slugs_with_the_same_value_are_equal()
    {
        var first = Slug.Create("clean-architecture");
        var second = Slug.FromTitle("Clean Architecture");

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
    }

    [Fact]
    public void A_slug_longer_than_the_limit_is_rejected()
        => Assert.Throws<DomainException>(() => Slug.Create(new string('a', Slug.MaxLength + 1)));
}
