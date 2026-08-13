using CodeSolution.Core.Vehicle;

namespace CodeSolution.Core.Fees;

public interface ITollCalculator
{
    /// <summary>
    /// Calculates the congestion tax fee for a single toll station passage.
    /// </summary>
    int GetTollFee(IVehicle vehicle, DateTime passage);

    /// <summary>
    /// Calculates the total congestion tax for a vehicle for one calendar day,
    /// given all of its toll station passages that day. Passages within 60 minutes
    /// of each other are grouped and only the highest fee within the group is charged.
    /// The result is capped at the daily maximum fee.
    /// </summary>
    int GetTollFee(IVehicle vehicle, IReadOnlyList<DateTime> passages);
}
