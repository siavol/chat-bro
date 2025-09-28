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
    public async Task<IActionResult> Get()
    {
        logger.LogInformation("Get restaurants from site");
        var html = await scrapper.Scrape();
        var restaurants = parser.Parse(html);
        logger.LogDebug("Found {Count} restaurants", restaurants.Count);

        var result = new { restaurants };
        return Ok(result);
    }
}
