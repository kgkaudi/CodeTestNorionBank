using CodeSolution.Core.Fees;
using CodeSolution.Core.Holidays;
using CodeSolution.Core.Vehicle;

namespace CodeSolution.Tests.Unit;

public class TollCalculatorTests
{
    private readonly TollCalculator _calculator = new(
        new TollFeeSchedule(),
        new SwedishPublicHolidayProvider());

    [Fact]
    public void GetTollFee_ReturnsFee_ForRegularWeekdayPassage()
    {
        // 2013-01-02 is a regular non-holiday Wednesday.
        var passage = new DateTime(2013, 1, 2, 7, 0, 0);

        int fee = _calculator.GetTollFee(new Car(), passage);

        Assert.Equal(18, fee);
    }

    [Fact]
    public void GetTollFee_ReturnsZero_ForTollFreeVehicle()
    {
        var passage = new DateTime(2013, 1, 2, 7, 0, 0);

        int fee = _calculator.GetTollFee(new Motorbike(), passage);

        Assert.Equal(0, fee);
    }

    [Theory]
    [InlineData(typeof(Motorbike))]
    [InlineData(typeof(Tractor))]
    [InlineData(typeof(EmergencyVehicle))]
    [InlineData(typeof(Diplomat))]
    [InlineData(typeof(ForeignVehicle))]
    [InlineData(typeof(MilitaryVehicle))]
    public void GetTollFee_ReturnsZero_ForEveryTollFreeVehicleType(Type vehicleType)
    {
        // Only Motorbike is exercised above - this proves the exemption rule
        // holds for every VehicleType listed in TollFreeVehicles, not just one.
        var vehicle = (IVehicle)Activator.CreateInstance(vehicleType)!;
        var passage = new DateTime(2013, 1, 2, 7, 0, 0);

        int fee = _calculator.GetTollFee(vehicle, passage);

        Assert.Equal(0, fee);
    }

    [Fact]
    public void GetTollFee_ReturnsZero_OnWeekend()
    {
        // 2013-01-05 is a Saturday.
        var passage = new DateTime(2013, 1, 5, 7, 0, 0);

        int fee = _calculator.GetTollFee(new Car(), passage);

        Assert.Equal(0, fee);
    }

    [Fact]
    public void GetTollFee_ReturnsZero_InJuly()
    {
        var passage = new DateTime(2013, 7, 15, 7, 0, 0);

        int fee = _calculator.GetTollFee(new Car(), passage);

        Assert.Equal(0, fee);
    }

    [Fact]
    public void GetTollFee_ReturnsZero_OnPublicHoliday()
    {
        // Christmas Day.
        var passage = new DateTime(2013, 12, 25, 7, 0, 0);

        int fee = _calculator.GetTollFee(new Car(), passage);

        Assert.Equal(0, fee);
    }

    [Fact]
    public void GetTollFee_ReturnsZero_OnDayBeforePublicHoliday()
    {
        // 2013-03-28 is the day before Good Friday (2013-03-29) and is
        // therefore toll-free even though it isn't a holiday itself.
        var passage = new DateTime(2013, 3, 28, 7, 0, 0);

        int fee = _calculator.GetTollFee(new Car(), passage);

        Assert.Equal(0, fee);
    }

    [Fact]
    public void GetTollFee_ForMultiplePassages_ChargesOnlyHighestFeeWithin60Minutes()
    {
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 6, 0, 0),  // 8 SEK
            new DateTime(2013, 1, 2, 6, 45, 0), // 13 SEK, 45 min after first passage
        };

        int fee = _calculator.GetTollFee(new Car(), passages);

        Assert.Equal(13, fee);
    }

    [Fact]
    public void GetTollFee_ForMultiplePassages_GroupsPassagesExactly60MinutesApart()
    {
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 6, 0, 0), // 8 SEK
            new DateTime(2013, 1, 2, 7, 0, 0), // 18 SEK, exactly 60 minutes after first passage
        };

        int fee = _calculator.GetTollFee(new Car(), passages);

        // The tolerance check is "<= 60 minutes", so exactly 60 minutes
        // still counts as the same window - only the highest fee is charged.
        Assert.Equal(18, fee);
    }

    [Fact]
    public void GetTollFee_ForMultiplePassages_ChargesSeparatelyWhenMoreThan60MinutesApart()
    {
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 6, 0, 0),  // 8 SEK
            new DateTime(2013, 1, 2, 7, 30, 0), // 18 SEK, 90 min after first
        };

        int fee = _calculator.GetTollFee(new Car(), passages);

        Assert.Equal(26, fee);
    }

    [Fact]
    public void GetTollFee_ForMultiplePassages_ChargesSeparatelyWhen61MinutesApart()
    {
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 6, 0, 0), // 8 SEK
            new DateTime(2013, 1, 2, 7, 1, 0), // 18 SEK, 61 minutes after first passage
        };

        int fee = _calculator.GetTollFee(new Car(), passages);

        // One minute past the tolerance window - charged as a separate passage.
        Assert.Equal(26, fee);
    }

    [Fact]
    public void GetTollFee_ForMultiplePassages_WindowIsAnchoredToFirstPassageNotMostRecentPassage()
    {
        // Documents a deliberate design choice: the 60-minute window is
        // measured from the passage that opened it, not re-anchored to the
        // most recent passage in the group. A passage 45 minutes after the
        // window opened, followed by another 45 minutes after *that* one
        // (90 minutes after the window opened), starts a new window rather
        // than extending the first one indefinitely.
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 6, 0, 0),  // opens window 1, 8 SEK
            new DateTime(2013, 1, 2, 6, 45, 0), // 45 min after window 1 start -> still window 1, 13 SEK
            new DateTime(2013, 1, 2, 7, 30, 0), // 90 min after window 1 start -> opens window 2, 18 SEK
        };

        int fee = _calculator.GetTollFee(new Car(), passages);

        // window 1 = max(8, 13) = 13, window 2 = 18
        Assert.Equal(31, fee);
    }

    [Fact]
    public void GetTollFee_ForMultiplePassages_HandlesUnsortedInput()
    {
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 7, 30, 0), // 18 SEK
            new DateTime(2013, 1, 2, 6, 0, 0),  // 8 SEK, out of order in the input
        };

        int fee = _calculator.GetTollFee(new Car(), passages);

        Assert.Equal(26, fee);
    }

    [Fact]
    public void GetTollFee_ForMultiplePassages_SumsMultipleSeparateWindowsBelowCap()
    {
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 6, 0, 0),  // window 1: 8
            new DateTime(2013, 1, 2, 8, 0, 0),  // window 2: 13
            new DateTime(2013, 1, 2, 18, 0, 0), // window 3: 8
        };

        int fee = _calculator.GetTollFee(new Car(), passages);

        // 8 + 13 + 8, comfortably under the 60 SEK cap - proves summation
        // across more than two windows, not just two.
        Assert.Equal(29, fee);
    }

    [Fact]
    public void GetTollFee_IsCappedAtDailyMaximum()
    {
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 6, 0, 0),   // 8
            new DateTime(2013, 1, 2, 8, 0, 0),   // 13
            new DateTime(2013, 1, 2, 15, 30, 0), // 18
            new DateTime(2013, 1, 2, 17, 0, 0),  // 13
        };

        int fee = _calculator.GetTollFee(new Car(), passages);

        Assert.Equal(52, fee);
    }

    [Fact]
    public void GetTollFee_ForMultiplePassages_ReturnsZero_ForTollFreeVehicle()
    {
        // The array overload has its own IsTollFree short-circuit before the
        // grouping loop runs - this exercises that path specifically, not
        // just the single-passage overload above.
        var passages = new[]
        {
            new DateTime(2013, 1, 2, 6, 0, 0),
            new DateTime(2013, 1, 2, 7, 0, 0),
        };

        int fee = _calculator.GetTollFee(new Motorbike(), passages);

        Assert.Equal(0, fee);
    }

    [Fact]
    public void GetTollFee_ReturnsZero_WhenNoPassages()
    {
        int fee = _calculator.GetTollFee(new Car(), Array.Empty<DateTime>());

        Assert.Equal(0, fee);
    }

    [Fact]
    public void GetTollFee_ThrowsArgumentNullException_WhenVehicleIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.GetTollFee(null!, new DateTime(2013, 1, 2, 7, 0, 0)));
    }

    [Fact]
    public void GetTollFee_ThrowsArgumentNullException_WhenPassagesIsNull()
    {
        Assert.Throws<ArgumentNullException>(() =>
            _calculator.GetTollFee(new Car(), (IReadOnlyList<DateTime>)null!));
    }
}