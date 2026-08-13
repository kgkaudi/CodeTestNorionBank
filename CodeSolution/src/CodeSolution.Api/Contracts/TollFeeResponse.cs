namespace CodeSolution.Api.Contracts;

public sealed class TollFeeResponse
{
    public string VehicleType { get; set; } = string.Empty;
    public int TotalFee { get; set; }
    public string Currency { get; set; } = "SEK";
}
