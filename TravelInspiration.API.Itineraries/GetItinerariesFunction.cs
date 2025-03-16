using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelInspiration.API.Itineraries.DbContexts;
using TravelInspiration.API.Itineraries.Model;

namespace TravelInspiration.API.Itineraries;

public class GetItinerariesFunction
{
    private readonly ILogger<GetItinerariesFunction> _logger;
    private readonly TravelInspirationDbContext _dbContext;

    public GetItinerariesFunction(ILogger<GetItinerariesFunction> logger, TravelInspirationDbContext dbContext)
    {
        _logger = logger;
        _dbContext = dbContext;
    }

    [Function("GetItinerariesFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "itineraries")]
        HttpRequest req)

    {
        string? searchForValue = req.Query["SearchFor"];

        var itineraryEntities = await _dbContext.Itineraries
            .Where(i =>
                searchForValue == null ||
                i.Name.Contains(searchForValue) ||
                (i.Description != null && i.Description.Contains(searchForValue)))
            .ToListAsync();

        var itineraryDtos = itineraryEntities.Select(i => new ItineraryDto()
        {
            Id = i.Id,
            Description = i.Description,
            Name = i.Name,
            UserId = i.UserId
        });

        return new OkObjectResult(itineraryDtos);
    }
}