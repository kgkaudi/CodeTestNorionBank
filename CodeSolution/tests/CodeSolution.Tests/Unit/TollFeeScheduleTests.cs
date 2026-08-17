using CodeSolution.Core.Fees;

namespace CodeSolution.Tests.Unit;

public class TollFeeScheduleTests
{
    private readonly TollFeeSchedule _schedule = new();

    [Theory]
    [InlineData(6, 0, 8)]
    [InlineData(6, 29, 8)]
    [InlineData(6, 30, 13)]
    [InlineData(6, 59, 13)]
    [InlineData(7, 0, 18)]
    [InlineData(7, 59, 18)]
    [InlineData(8, 0, 13)]
    [InlineData(8, 29, 13)]
    [InlineData(8, 30, 8)]
    [InlineData(14, 59, 8)]
    [InlineData(15, 0, 13)]
    [InlineData(15, 29, 13)]
    [InlineData(15, 30, 18)]
    [InlineData(16, 59, 18)]
    [InlineData(17, 0, 13)]
    [InlineData(17, 59, 13)]
    [InlineData(18, 0, 8)]
    [InlineData(18, 29, 8)]
    [InlineData(18, 30, 0)]
    [InlineData(5, 59, 0)]
    [InlineData(0, 0, 0)]
    public void GetFee_ReturnsExpectedFee(int hour, int minute, int expectedFee)
    {
        var time = new TimeOnly(hour, minute);

        int fee = _schedule.GetFee(time);

        Assert.Equal(expectedFee, fee);
    }

    [Theory]
    [InlineData(6, 29, 59, 8)]   // last second still in the 06:00-06:29 band
    [InlineData(6, 30, 0, 13)]   // first second of the next band
    [InlineData(18, 29, 59, 8)]  // last second still toll-charged before the free period
    [InlineData(18, 30, 0, 0)]   // first second of the toll-free evening period
    public void GetFee_ReturnsExpectedFee_AtSecondBoundary(int hour, int minute, int second, int expectedFee)
    {
        // Bands are defined down to the second (e.g. 06:29:59), not just the
        // minute. Testing only whole minutes (as above) proves the switch
        // happens somewhere in the right minute, but not that the boundary
        // sits exactly on the last second of it - a band accidentally
        // shortened to end at :29:00 instead of :29:59 would still pass
        // every minute-level test above.
        var time = new TimeOnly(hour, minute, second);

        int fee = _schedule.GetFee(time);

        Assert.Equal(expectedFee, fee);
    }
}