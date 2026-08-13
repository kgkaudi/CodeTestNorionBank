using CodeSolution.Core.Vehicle;

namespace CodeSolution.Core.Fees;

/// <summary>
/// Which <see cref="VehicleType"/>s are exempt from congestion tax entirely,
/// regardless of time or date.
/// </summary>
public static class TollFreeVehicles
{
    private static readonly HashSet<VehicleType> ExemptTypes = new()
    {
        VehicleType.Motorbike,
        VehicleType.Tractor,
        VehicleType.Emergency,
        VehicleType.Diplomat,
        VehicleType.Foreign,
        VehicleType.Military,
    };

    public static bool IsTollFree(IVehicle vehicle) => ExemptTypes.Contains(vehicle.Type);
}
