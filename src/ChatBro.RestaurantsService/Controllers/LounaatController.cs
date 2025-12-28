
using ChatBro.RestaurantsService.Clients;
using Microsoft.AspNetCore.Mvc;

namespace ChatBro.RestaurantsService.Controllers;

[ApiController]
[Route("lounaat")]
public class LounaatController(LounaatClient client) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? date, [FromQuery] double lat, [FromQuery] double lng)
    {
        var dateToUse = date ?? DateOnly.FromDateTime(DateTime.Now);
        var restaurants = await client.GetRestaurants(dateToUse, lat, lng);
        var result = new { restaurants };
        return Ok(result);
    }
}
