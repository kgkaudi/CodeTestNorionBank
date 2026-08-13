namespace CodeSolution.Core.Vehicle;

/// <summary>
/// Creates <see cref="IVehicle"/> instances from a plain-text vehicle type name,
/// e.g. as received from an API request.
/// </summary>
public static class VehicleFactory
{
    public static bool TryCreate(string? vehicleType, out IVehicle? vehicle)
    {
        vehicle = (vehicleType ?? string.Empty).Trim().ToLowerInvariant() switch
        {
            "car" => new Car(),
            "motorbike" => new Motorbike(),
            "tractor" => new Tractor(),
            "emergency" => new EmergencyVehicle(),
            "diplomat" => new Diplomat(),
            "foreign" => new ForeignVehicle(),
            "military" => new MilitaryVehicle(),
            _ => null
        };

        return vehicle is not null;
    }
}
