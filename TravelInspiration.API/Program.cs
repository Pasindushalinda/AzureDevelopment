using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using TravelInspiration.API;
using TravelInspiration.API.Shared.Slices;
using Microsoft.Extensions.Azure;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddProblemDetails();
builder.Services.AddHttpClient();
builder.Services.AddHttpContextAccessor(); 
   
builder.Services.RegisterApplicationServices();
builder.Services.RegisterPersistenceServices(builder.Configuration);
builder.Services.AddAzureClients(clientBuilder =>
{
    clientBuilder.AddBlobServiceClient(builder.Configuration["ConnectionStrings:TravelInspirationStorageConnection1:blobServiceUri"]!).WithName("ConnectionStrings:TravelInspirationStorageConnection1");
    clientBuilder.AddQueueServiceClient(builder.Configuration["ConnectionStrings:TravelInspirationStorageConnection1:queueServiceUri"]!).WithName("ConnectionStrings:TravelInspirationStorageConnection1");
    clientBuilder.AddTableServiceClient(builder.Configuration["ConnectionStrings:TravelInspirationStorageConnection1:tableServiceUri"]!).WithName("ConnectionStrings:TravelInspirationStorageConnection1");
});

builder.Services.AddApplicationInsightsTelemetry(new Microsoft.ApplicationInsights.AspNetCore.Extensions.ApplicationInsightsServiceOptions
{
    ConnectionString = builder.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"]
});

var app = builder.Build();

// Configure the HTTP request pipeline.
app.UseHttpsRedirection();
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler();
}
app.UseStatusCodePages();
 
app.MapSliceEndpoints();

app.Run();