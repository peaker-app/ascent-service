using AscentService.Infrastructure.ExternalServices;
using Common.Contracts.Profiles;
using MassTransit;
using Microsoft.Extensions.Caching.Memory;

namespace AscentService.Infrastructure.Messaging.Consumers;

internal sealed class ProfileUpdatedConsumer(IMemoryCache cache) : IConsumer<ProfileUpdated>
{
    public Task Consume(ConsumeContext<ProfileUpdated> context)
    {
        CachingProfileDirectory.Evict(cache, context.Message.UserId);

        return Task.CompletedTask;
    }
}
