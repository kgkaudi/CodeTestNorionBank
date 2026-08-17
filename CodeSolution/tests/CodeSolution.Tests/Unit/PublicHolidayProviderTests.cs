using CodeSolution.Core.Holidays;

namespace CodeSolution.Tests.Unit;

public class PublicHolidayProviderTests
{
    private readonly SwedishPublicHolidayProvider _provider = new();

    [Theory]
    [InlineData(2013, 1, 1)]
    [InlineData(2013, 3, 29)]
    [InlineData(2013, 4, 1)]
    [InlineData(2013, 5, 1)]
    [InlineData(2013, 5, 9)]
    [InlineData(2013, 6, 6)]
    [InlineData(2013, 6, 21)]
    [InlineData(2013, 11, 1)]
    [InlineData(2013, 12, 25)]
    [InlineData(2013, 12, 26)]
    public void IsPublicHoliday_ReturnsTrue_ForKnownHolidays(int year, int month, int day)
    {
        Assert.True(_provider.IsPublicHoliday(new DateOnly(year, month, day)));
    }

    [Fact]
    public void IsPublicHoliday_ReturnsFalse_ForRegularDay()
    {
        Assert.False(_provider.IsPublicHoliday(new DateOnly(2013, 1, 2)));
    }

    [Theory]
    [InlineData(2013, 3, 28)]  // day before Good Friday (2013-03-29)
    [InlineData(2013, 12, 24)] // Christmas Eve — day before Christmas Day
    [InlineData(2013, 12, 31)] // New Year's Eve — day before New Year's Day
    [InlineData(2013, 4, 30)]  // Walpurgis Night — day before May Day
    [InlineData(2013, 5, 8)]   // day before Ascension Day
    public void IsPublicHoliday_ReturnsFalse_ForDayBeforeAHoliday(int year, int month, int day)
    {
        // The "day before a holiday is toll-free" rule is applied once,
        // generically, in TollCalculator.IsTollFreeDate - this provider
        // should only ever report actual holidays, never their eves.
        Assert.False(_provider.IsPublicHoliday(new DateOnly(year, month, day)));
    }

    [Theory]
    [InlineData(2012, 12, 25)]
    [InlineData(2014, 1, 1)]
    public void IsPublicHoliday_ReturnsFalse_ForDateOutsideSupportedYear(int year, int month, int day)
    {
        // SwedishPublicHolidayProvider currently only has data for 2013
        // (see the class remarks). This documents that known limitation
        // rather than leaving it implicit - see README "What I'd do next"
        // for the plan to replace this with a real multi-year calendar.
        Assert.False(_provider.IsPublicHoliday(new DateOnly(year, month, day)));
    }
}