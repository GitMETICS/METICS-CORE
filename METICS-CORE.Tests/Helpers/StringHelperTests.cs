using FluentAssertions;
using webMetics.Helpers;
using Xunit;

namespace webMetics.Tests.Helpers;

public class StringHelperTests
{
    [Theory]
    [InlineData("José", "Jose")]
    [InlineData("Área", "Area")]
    [InlineData("Ángel", "Angel")]
    [InlineData("Héctor", "Hector")]
    [InlineData("niño", "nino")]
    public void RemoveAccents_SpanishText_RemovesDiacritics(string input, string expected)
    {
        StringHelper.RemoveAccents(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("hello")]
    [InlineData("WORLD")]
    [InlineData("abc123")]
    public void RemoveAccents_TextWithoutAccents_IsUnchanged(string input)
    {
        StringHelper.RemoveAccents(input).Should().Be(input);
    }

    [Fact]
    public void RemoveAccents_EmptyString_ReturnsEmptyString()
    {
        StringHelper.RemoveAccents(string.Empty).Should().BeEmpty();
    }
}
