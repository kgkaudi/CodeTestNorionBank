namespace CodeSolution.Core.Vehicle;

public sealed class EmergencyVehicle : IVehicle
{
    public VehicleType Type => VehicleType.Emergency;
}
