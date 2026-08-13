using Microsoft.AspNetCore.Mvc;
using CodeSolution.Api.Contracts;
using CodeSolution.Core.Fees;
using CodeSolution.Core.Vehicle;

namespace CodeSolution.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class TollFeeController : ControllerBase
{
    private readonly ITollCalculator _calculator;

    public TollFeeController(ITollCalculator calculator)
    {
        _calculator = calculator;
    }

    /// <summary>
    /// Calculates the total congestion tax for a vehicle's toll station passages
    /// on a single day.
    /// </summary>
    [HttpPost("calculate")]
    [ProducesResponseType(typeof(TollFeeResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public ActionResult<TollFeeResponse> Calculate([FromBody] TollFeeRequest request)
    {
        if (request.Passages is null || request.Passages.Count == 0)
        {
            return BadRequest("At least one passage timestamp is required.");
        }

        if (!VehicleFactory.TryCreate(request.VehicleType, out IVehicle? vehicle) || vehicle is null)
        {
            return BadRequest($"Unknown vehicle type: '{request.VehicleType}'.");
        }

        int fee = _calculator.GetTollFee(vehicle, request.Passages);

        return Ok(new TollFeeResponse
        {
            VehicleType = vehicle.Type.ToString(),
            TotalFee = fee,
            Currency = "SEK"
        });
    }
}
