using System.Text.Json;
using Azure.Storage.Queues;
using MediatR;
using TravelInspiration.API.Shared.Slices;

namespace TravelInspiration.API.Features.Destinations;

public sealed class ProcessDestinationImageChanges : ISlice
{
    public void AddEndpoint(IEndpointRouteBuilder endpointRouteBuilder)
    {
        endpointRouteBuilder.MapGet("api/processdestinationimagechanges",
             (IMediator mediator,
                CancellationToken cancellationToken) =>
             {
                 return mediator.Send(
                     new ProcessDestinationImageChangesQuery(),
                     cancellationToken);
             });
    }

    public sealed class ProcessDestinationImageChangesQuery() : IRequest<IResult>
    {
    }

    public sealed class MessageDto
    {
        public required string Action { get; set; }
        public required string BlobName { get; set; }
    }

    public sealed class ProcessDestinationImageChangesHandler(QueueServiceClient queueServiceClient)
        :
       IRequestHandler<ProcessDestinationImageChangesQuery, IResult>
    {
        private readonly QueueServiceClient _queueServiceClient = queueServiceClient;

        public async Task<IResult> Handle(ProcessDestinationImageChangesQuery request,
            CancellationToken cancellationToken)
        {
            var queueClient = _queueServiceClient.GetQueueClient("image-message-queue");
            var messageDto = new List<MessageDto>();

            var messages = await queueClient.ReceiveMessagesAsync(maxMessages: 10, cancellationToken: cancellationToken);

            foreach (var message in messages.Value)
            {
                var messageFromQueue = JsonSerializer.Deserialize<MessageDto>(message.MessageText);
                if (messageFromQueue != null)
                {
                    messageDto.Add(messageFromQueue);
                    await queueClient.DeleteMessageAsync(message.MessageId, message.PopReceipt, cancellationToken);
                }
            }
            return Results.Ok(messageDto);
        }

    }
}