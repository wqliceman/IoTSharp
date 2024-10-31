using EasyCaching.Core;
using IoTSharp.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IoTSharp.EventBus.NServiceBus
{
    public class NSBusSubscriber : EventBusSubscriber, ISubscriber
    {
        public NSBusSubscriber(ILogger<EventBusSubscriber> logger, IServiceScopeFactory scopeFactor
           , IStorage storage, IEasyCachingProviderFactory factory, EventBusOption eventBusOption
            ) : base(logger, scopeFactor, storage, factory, eventBusOption)
        {
        }
    }
}