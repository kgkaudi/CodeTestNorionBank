using CodeSolution.Core.Holidays;
using CodeSolution.Core.Vehicle;

namespace CodeSolution.Core.Fees;

/// <summary>
/// Calculates the Gothenburg congestion tax ("trängselskatt") for vehicle
/// passages through toll stations.
/// </summary>
public sealed class TollCalculator : ITollCalculator
{
    private const int DailyMaximumFee = 60;
    private const int ToleranceWindowMinutes = 60;

    private readonly TollFeeSchedule _schedule;
    private readonly IPublicHolidayProvider _holidayProvider;

    public TollCalculator(TollFeeSchedule schedule, IPublicHolidayProvider holidayProvider)
    {
        _schedule = schedule;
        _holidayProvider = holidayProvider;
    }

    public int GetTollFee(IVehicle vehicle, DateTime passage)
    {
        ArgumentNullException.ThrowIfNull(vehicle);

        if (TollFreeVehicles.IsTollFree(vehicle))
        {
            return 0;
        }

        var date = DateOnly.FromDateTime(passage);
        if (IsTollFreeDate(date))
        {
            return 0;
        }

        return _schedule.GetFee(TimeOnly.FromDateTime(passage));
    }

    public int GetTollFee(IVehicle vehicle, IReadOnlyList<DateTime> passages)
    {
        ArgumentNullException.ThrowIfNull(vehicle);
        ArgumentNullException.ThrowIfNull(passages);

        if (passages.Count == 0)
        {
            return 0;
        }

        if (TollFreeVehicles.IsTollFree(vehicle))
        {
            return 0;
        }

        List<DateTime> sortedPassages = passages.OrderBy(p => p).ToList();

        int totalFee = 0;
        DateTime windowStart = sortedPassages[0];
        int windowMaxFee = GetTollFee(vehicle, windowStart);

        for (int i = 1; i < sortedPassages.Count; i++)
        {
            DateTime passage = sortedPassages[i];

            if ((passage - windowStart).TotalMinutes <= ToleranceWindowMinutes)
            {
                windowMaxFee = Math.Max(windowMaxFee, GetTollFee(vehicle, passage));
            }
            else
            {
                totalFee += windowMaxFee;
                windowStart = passage;
                windowMaxFee = GetTollFee(vehicle, passage);
            }
        }

        totalFee += windowMaxFee;

        return Math.Min(totalFee, DailyMaximumFee);
    }

    /// <summary>
    /// A date is toll-free if it's a weekend, falls in July, is a public holiday,
    /// or is the day immediately before a public holiday.
    /// </summary>
    private bool IsTollFreeDate(DateOnly date)
    {
        if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
        {
            return true;
        }

        if (date.Month == 7)
        {
            return true;
        }

        if (_holidayProvider.IsPublicHoliday(date))
        {
            return true;
        }

        if (_holidayProvider.IsPublicHoliday(date.AddDays(1)))
        {
            return true;
        }

        return false;
    }
}
