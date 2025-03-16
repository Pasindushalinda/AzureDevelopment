using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TravelInspiration.API.Itineraries.DbContexts;
using TravelInspiration.API.Itineraries.Model;
using Microsoft.Extensions.Configuration;

namespace TravelInspiration.API.Itineraries;

public class GetItinerariesFunction
{
    private readonly ILogger<GetItinerariesFunction> _logger;
    private readonly TravelInspirationDbContext _dbContext;
    private readonly IConfiguration _configuration;

    public GetItinerariesFunction(ILogger<GetItinerariesFunction> logger, 
        TravelInspirationDbContext dbContext,
        IConfiguration configuration)
    {
        _logger = logger;
        _dbContext = dbContext;
        _configuration = configuration;
    }

    [Function("GetItinerariesFunction")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "itineraries")]
        HttpRequest req)

    {
        string? searchForValue = req.Query["SearchFor"];

        if (!int.TryParse(_configuration["MaximumAmountOfItinerariesToReturn"],
            out int maximumAmountOfItinerariesToReturn))
        {
            throw new Exception("MaximumAmountOfItinerariesToReturn setting is missing or its value is not a valid integer.");
        }

        var itineraryEntities = await _dbContext.Itineraries
            .Where(i =>
                searchForValue == null ||
                i.Name.Contains(searchForValue) ||
                (i.Description != null && i.Description.Contains(searchForValue)))
            .Take(maximumAmountOfItinerariesToReturn)
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