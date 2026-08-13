namespace CodeSolution.Api.Contracts;

public sealed class TollFeeRequest
{
    /// <summary>
    /// Vehicle type: "car", "motorbike", "tractor", "emergency", "diplomat",
    /// "foreign" or "military" (case-insensitive).
    /// </summary>
    public string VehicleType { get; set; } = string.Empty;

    /// <summary>
    /// All toll station passage timestamps for this vehicle on a single calendar day.
    /// </summary>
    public List<DateTime> Passages { get; set; } = new();
}
