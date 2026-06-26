using System.Linq;
using PowerOffScreensaver.Localization;
using Xunit;

namespace PowerOffScreensaver.Tests;

public class LocalizationTests
{
    private static readonly string[] Expected =
        { "en", "ru", "de", "fr", "es", "it", "pt", "pl", "zh" };

    [Fact]
    public void Exactly_Nine_Languages_Are_Present()
    {
        Assert.Equal(9, Strings.All.Count);
        Assert.Equal(Expected.OrderBy(x => x), Strings.All.Keys.OrderBy(x => x));
    }

    [Fact]
    public void Chinese_Is_Present_And_Ukrainian_Is_Gone()
    {
        Assert.True(Strings.All.ContainsKey("zh"));
        Assert.False(Strings.All.ContainsKey("uk"));
    }

    [Theory]
    [InlineData("en")]
    [InlineData("ru")]
    [InlineData("de")]
    [InlineData("fr")]
    [InlineData("es")]
    [InlineData("it")]
    [InlineData("pt")]
    [InlineData("pl")]
    [InlineData("zh")]
    public void Every_Language_Has_Complete_NonEmpty_Strings(string code)
    {
        var s = Strings.All[code];
        Assert.False(string.IsNullOrWhiteSpace(s.Flag));
        Assert.False(string.IsNullOrWhiteSpace(s.NativeName));
        Assert.Contains("{0}", s.WindowTitle); // version placeholder preserved
        Assert.False(string.IsNullOrWhiteSpace(s.LockOnExit));
        Assert.False(string.IsNullOrWhiteSpace(s.DdcCi));
        Assert.False(string.IsNullOrWhiteSpace(s.DelayMs));
        Assert.False(string.IsNullOrWhiteSpace(s.TestBtn));
        Assert.False(string.IsNullOrWhiteSpace(s.OkBtn));
        Assert.False(string.IsNullOrWhiteSpace(s.CancelBtn));
        Assert.False(string.IsNullOrWhiteSpace(s.CheckBtn));
        Assert.False(string.IsNullOrWhiteSpace(s.LockOnExitHint));
        Assert.False(string.IsNullOrWhiteSpace(s.DelayHint));
        Assert.False(string.IsNullOrWhiteSpace(s.DdcCiHint));
    }

    [Fact]
    public void Set_FallsBackToEnglish_ForUnknownLanguage()
    {
        Strings.Set("xx");
        Assert.Equal("en", Strings.Current);

        Strings.Set("zh");
        Assert.Equal("zh", Strings.Current);
        Strings.Set("en"); // restore
    }
}
