using Azure.Identity;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TravelInspiration.API.Itineraries.DbContexts;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

// Application Insights isn't enabled by default. See https://aka.ms/AAt8mw4.
builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

var credential = new DefaultAzureCredential();

// Get a token to access Azure SQL
var accessTokenResponse = credential.GetToken(
    new Azure.Core.TokenRequestContext(["https://database.windows.net/.default"]));

var sqlConnection = new SqlConnection(
    builder.Configuration.GetConnectionString("TravelInspirationDbConnection"))
{
    AccessToken = accessTokenResponse.Token
};

builder.Services.AddDbContext<TravelInspirationDbContext>(options =>
    options.UseSqlServer(sqlConnection,
        sqlOptions => sqlOptions.EnableRetryOnFailure()));

//builder.Services.AddDbContext<TravelInspirationDbContext>(options =>
//    options.UseSqlServer(
//        builder.Configuration.GetConnectionString("TravelInspirationDbConnection"),
//        sqlOptions => sqlOptions.EnableRetryOnFailure()));

builder.Build().Run();