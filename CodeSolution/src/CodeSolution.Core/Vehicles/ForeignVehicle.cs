namespace CodeSolution.Core.Vehicle;

public sealed class ForeignVehicle : IVehicle
{
    public VehicleType Type => VehicleType.Foreign;
}
