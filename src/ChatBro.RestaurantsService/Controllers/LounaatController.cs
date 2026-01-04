
using System.ComponentModel.DataAnnotations;
using ChatBro.RestaurantsService.Clients;
using Microsoft.AspNetCore.Mvc;

namespace ChatBro.RestaurantsService.Controllers;

[ApiController]
[Route("lounaat")]
public class LounaatController(LounaatClient client) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get(
        [FromQuery] DateOnly? date, 
        [FromQuery, Required] double lat, 
        [FromQuery, Required] double lng)
    {
        if (lat < -90 || lat > 90 || lng < -180 || lng > 180)
        {
            return BadRequest(new
            {
                error = "Invalid coordinates.",
                latitude = lat,
                longitude = lng,
                details = "Latitude must be between -90 and 90. Longitude must be between -180 and 180."
            });
        }
        var dateToUse = date ?? DateOnly.FromDateTime(DateTime.Now);
        var restaurants = await client.GetRestaurants(dateToUse, lat, lng);
        var result = new { restaurants };
        return Ok(result);
    }
}
