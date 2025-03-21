﻿using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using MediatR;
using System.Text;
using System.Text.Json;
using TravelInspiration.API.Shared.Slices;

namespace TravelInspiration.API.Features.Destinations;

public class UpdateDestinationImages : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapPut("api/destinations/{destinationId}/images",
            (int destinationId,
            UpdateDestinationImagesCommand updateDestinationImagesCommand,
            IMediator mediator,
            CancellationToken cancellationToken) =>
            {
                // make sure the destinationId from the Uri is used
                updateDestinationImagesCommand.DestinationId = destinationId;
                return mediator.Send(
                  updateDestinationImagesCommand,
                  cancellationToken);
            });
    }

    public sealed class UpdateDestinationImagesCommand : IRequest<IResult>
    {
        public sealed class ImageToUpdate
        {
            public required string Name { get; set; }
            public required string ImageBytes { get; set; }
        }
        public int DestinationId { get; set; }
        public List<ImageToUpdate> ImagesToUpdate { get; set; } = [];
    }


    public sealed class UpdateDestinationImagesCommandHandler(IConfiguration configuration,
        BlobServiceClient blobServiceClient,
        QueueServiceClient queueServiceClient) :
        IRequestHandler<UpdateDestinationImagesCommand, IResult>
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly BlobServiceClient _blobServiceClient = blobServiceClient;
        private readonly QueueServiceClient _queueServiceClient = queueServiceClient;

        public async Task<IResult> Handle(UpdateDestinationImagesCommand request,
            CancellationToken cancellationToken)
        {
            var destinationContainerClient = _blobServiceClient.GetBlobContainerClient("destination-images");
            var destinationQueueClient = _queueServiceClient.GetQueueClient("image-message-queue");

            foreach (var imageToUpdate in request.ImagesToUpdate)
            {
                var blobClient = destinationContainerClient.GetBlobClient(imageToUpdate.Name);

                if (!blobClient.Exists())
                {
                    using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(imageToUpdate.ImageBytes)))
                    {
                        await blobClient.UploadAsync(stream,
                            new BlobUploadOptions()
                            {
                                Tags = new Dictionary<string, string>
                                { { "DestinationIdentifier", request.DestinationId.ToString() }  }
                            },
                            cancellationToken);
                    }

                    var message = new
                    {
                        Action = "ImageAdded",
                        BlobName = imageToUpdate.Name,
                    };

                    await destinationQueueClient.SendMessageAsync(JsonSerializer.Serialize(message), cancellationToken: cancellationToken);
                }
                else
                {
                    var blobTags = blobClient.GetTags(cancellationToken: cancellationToken);

                    if (blobTags.Value.Tags.TryGetValue("DestinationIdentifier", out var destinationId) && destinationId == request.DestinationId.ToString())
                    {
                        using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(imageToUpdate.ImageBytes)))
                        {
                            await blobClient.UploadAsync(stream,
                                new BlobUploadOptions()
                                {
                                    Tags = new Dictionary<string, string>
                                    { { "DestinationIdentifier", request.DestinationId.ToString() }  }
                                },
                                cancellationToken);
                        }
                    }

                    var message = new
                    {
                        Action = "ImageUpdated",
                        BlobName = imageToUpdate.Name,
                    };

                    await destinationQueueClient.SendMessageAsync(JsonSerializer.Serialize(message), cancellationToken: cancellationToken);
                }


            }
            return Results.Ok();
        }
    }
}
