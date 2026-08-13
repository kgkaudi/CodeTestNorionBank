namespace CodeSolution.Core.Fees;

/// <summary>
/// Maps a time of day to the congestion tax fee that applies at that time,
/// per the Gothenburg toll station schedule.
/// </summary>
public sealed class TollFeeSchedule
{
    private readonly record struct TimeBand(TimeSpan Start, TimeSpan End, int Fee);

    // End times are inclusive (e.g. 06:00-06:29 means up to and including 06:29:59).
    // Any time not covered by a band (18:30-05:59) is toll-free.
    private static readonly TimeBand[] Bands =
    {
        new(new TimeSpan(6, 0, 0),  new TimeSpan(6, 29, 59),  8),
        new(new TimeSpan(6, 30, 0), new TimeSpan(6, 59, 59), 13),
        new(new TimeSpan(7, 0, 0),  new TimeSpan(7, 59, 59), 18),
        new(new TimeSpan(8, 0, 0),  new TimeSpan(8, 29, 59), 13),
        new(new TimeSpan(8, 30, 0), new TimeSpan(14, 59, 59), 8),
        new(new TimeSpan(15, 0, 0), new TimeSpan(15, 29, 59), 13),
        new(new TimeSpan(15, 30, 0), new TimeSpan(16, 59, 59), 18),
        new(new TimeSpan(17, 0, 0), new TimeSpan(17, 59, 59), 13),
        new(new TimeSpan(18, 0, 0), new TimeSpan(18, 29, 59), 8),
    };

    public int GetFee(TimeOnly time)
    {
        TimeSpan t = time.ToTimeSpan();

        foreach (TimeBand band in Bands)
        {
            if (t >= band.Start && t <= band.End)
            {
                return band.Fee;
            }
        }

        return 0;
    }
}
