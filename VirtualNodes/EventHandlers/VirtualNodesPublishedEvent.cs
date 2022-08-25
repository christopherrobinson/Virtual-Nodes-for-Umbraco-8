using System;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace VirtualNodes.EventHandlers
{
    public class VirtualNodesPublishedEvent : INotificationHandler<ContentPublishedNotification>
    {
        private readonly IAppPolicyCache _runtimeCache;

        public VirtualNodesPublishedEvent(AppCaches appCaches)
        {
            _runtimeCache = appCaches.RuntimeCache;
        }

        public void Handle(ContentPublishedNotification notification)
        {
            Console.WriteLine("VirtualNodesPublishedEvent");
            _runtimeCache.ClearByKey("CachedVirtualNodes");
        }
    }
}
