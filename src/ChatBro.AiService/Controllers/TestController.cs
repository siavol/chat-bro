using ChatBro.RestaurantsService.KernelFunction;
using Microsoft.AspNetCore.Mvc;

namespace ChatBro.AiService.Controllers;

[ApiController]
[Route("test")]
public class TestController(
    RestaurantsServiceClient restaurantsServiceClient,
    ILogger<TestController> logger) : ControllerBase
{
    [HttpGet]
    [Route("restaurants")]
    public async Task<IActionResult> TestRestaurantsConnection()
    {
        try
        {
            var restaurants = await restaurantsServiceClient.GetRestaurantsAsync();
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            return restaurants != null
                ? Ok(new
                {
                    status = "ok",
                    restaurantsCount = restaurants.Count
                })
                : StatusCode(StatusCodes.Status500InternalServerError, new
                {
                    status = "error",
                    message = "Can not receive restaurants from service."
                });
        }
        catch (Exception e)
        {
            logger.LogError(e, "Error while getting restaurants from service.");
            throw;
        }
    }
}