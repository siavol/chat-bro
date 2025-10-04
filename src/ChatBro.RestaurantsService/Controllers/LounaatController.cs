using ChatBro.RestaurantsService.Clients;
using Microsoft.AspNetCore.Mvc;

namespace ChatBro.RestaurantsService.Controllers;

[ApiController]
[Route("lounaat")]
public class LounaatController(
    LounaatScrapper scrapper,
    LounaatParser parser,
    ILogger<LounaatController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get([FromQuery] DateOnly? date)
    {
        var dateToUse = date ?? DateOnly.FromDateTime(DateTime.Now);
        logger.LogInformation("Get restaurants from site for date {Date}", dateToUse);

        var html = await scrapper.Scrape(dateToUse);
        var restaurants = parser.Parse(html);
        logger.LogDebug("Found {Count} restaurants", restaurants.Count);

        var result = new { restaurants };
        return Ok(result);
    }

}
