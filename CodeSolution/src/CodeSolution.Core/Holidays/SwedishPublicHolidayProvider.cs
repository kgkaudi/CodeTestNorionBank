namespace CodeSolution.Core.Holidays;

/// <summary>
/// Provides Swedish public holidays (and Midsummer's Eve, which is conventionally
/// treated as toll-free even though it is not a formal red day).
///
/// The day immediately before a public holiday is automatically toll-free too -
/// see TollCalculator.IsTollFreeDate - so most "eve" days (New Year's Eve,
/// Walpurgis Night, Christmas Eve, etc.) don't need to be listed explicitly here,
/// only the holiday itself does.
///
/// NOTE: this list currently only covers 2013, matching the original test data.
/// For production use this should be replaced with a proper Swedish holiday
/// calculation (several of these move every year since they're Easter-relative)
/// or backed by an external holiday API/library, and extended for every year
/// the system needs to support.
/// </summary>
public sealed class SwedishPublicHolidayProvider : IPublicHolidayProvider
{
    private static readonly HashSet<DateOnly> Holidays = new()
    {
        new DateOnly(2013, 1, 1),   // New Year's Day
        new DateOnly(2013, 3, 29),  // Good Friday
        new DateOnly(2013, 4, 1),   // Easter Monday
        new DateOnly(2013, 5, 1),   // May Day
        new DateOnly(2013, 5, 9),   // Ascension Day
        new DateOnly(2013, 6, 6),   // National Day
        new DateOnly(2013, 6, 21),  // Midsummer's Eve
        new DateOnly(2013, 11, 1),  // All Saints' Day
        new DateOnly(2013, 12, 25), // Christmas Day
        new DateOnly(2013, 12, 26), // Boxing Day
    };

    public bool IsPublicHoliday(DateOnly date) => Holidays.Contains(date);
}
