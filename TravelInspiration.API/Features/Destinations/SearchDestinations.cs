using Azure.Data.Tables;
using Azure.Storage.Blobs;
using MediatR;
using TravelInspiration.API.Shared.Slices;

namespace TravelInspiration.API.Features.Destinations;

public sealed class SearchDestinations : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/destinations",
             (string? searchFor,
                IMediator mediator,
                CancellationToken cancellationToken) =>
             {
                 return mediator.Send(
                     new SearchDestinationsQuery(searchFor),
                     cancellationToken);
             });
    }

    public sealed class SearchDestinationsQuery(string? searchFor) : IRequest<IResult>
    {
        public string? SearchFor { get; } = searchFor;
    }

    public sealed class DestinationDto
    {
        public required int Id { get; set; }
        public required string Name { get; set; }
        public List<string> Urls { get; set; } = [];
    }

    public sealed class SearchDestinationsHandler(IConfiguration configuration,
        TableServiceClient tableServiceClient,
        BlobServiceClient blobServiceClient) :
       IRequestHandler<SearchDestinationsQuery, IResult>
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly TableServiceClient _tableServiceClient = tableServiceClient;
        private readonly BlobServiceClient blobServiceClient = blobServiceClient;

        public Task<IResult> Handle(SearchDestinationsQuery request,
            CancellationToken cancellationToken)
        {

            //var destinations = new List<DestinationDto> {
            //    new() { Id = 1, Name = "Antwerp, Belgium", ImageName = "antwerp.jpg" },
            //    new() { Id = 2, Name = "San Francisco, USA", ImageName = "sanfranciso.jpg" },
            //    new() { Id = 3, Name = "Sydney, Australia", ImageName = "sydney.jpg" },
            //    new() { Id = 4, Name = "Paris, France", ImageName = "paris.jpg" },
            //    new() { Id = 5, Name = "New Delhi, India", ImageName = "newdelhi.jpg" },
            //    new() { Id = 6, Name = "Tokyo, Japan", ImageName = "tokio.jpg" },
            //    new() { Id = 7, Name = "Cape Town, South Africa", ImageName = "capetown.jpg" },
            //    new() { Id = 8, Name = "Barcelona, Spain", ImageName = "barcelona.jpg" },
            //    new() { Id = 9, Name = "Toronto, Canada", ImageName = "toronto.jpg" }};

            var desTableClient = _tableServiceClient.GetTableClient("Destination");

            var filter = request.SearchFor == null ? "" : TableClient.CreateQueryFilter($"Name eq {request.SearchFor}");
            var amountToReturn = _configuration.GetValue<int>("Destinations:AmountToReturn");

            var destinations = desTableClient.Query<TableEntity>(filter,
                amountToReturn,
                ["Identifier", "Name"],
                cancellationToken);

            var paginatedData = destinations.AsPages().First()
                .Values.Select(d => new DestinationDto
                {
                    Id = d.GetInt32("Identifier") ?? -1,
                    Name = d.GetString("Name")
                }).ToList();

            if (paginatedData.Count == 0)
            {
                return Task.FromResult(Results.NotFound());
            }

            var desImageContainerClient = blobServiceClient.GetBlobContainerClient("destination-images");

            foreach (var destination in paginatedData)
            {
                var blobs = desImageContainerClient
                    .FindBlobsByTags($"DestinationIdentifier='{destination.Id}'");

                foreach (var blob in blobs)
                {
                    var blobClient = desImageContainerClient.GetBlobClient(blob.BlobName);
                    destination.Urls.Add(blobClient.Uri.AbsoluteUri);
                }


            }
            // return results, returning only the amount described in settings
            return Task.FromResult(Results.Ok(paginatedData));
        }

    }
}
