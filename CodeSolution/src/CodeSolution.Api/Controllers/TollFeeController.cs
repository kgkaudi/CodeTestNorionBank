using Microsoft.AspNetCore.Mvc;

namespace GothenburgTollFee.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TollFeeController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            message = "Gothenburg Toll Fee API"
        });
    }
}