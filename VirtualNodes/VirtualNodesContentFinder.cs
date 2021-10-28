using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Cache;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace VirtualNodes
{
    public class VirtualNodesContentFinder : IContentFinder
    {
        private readonly IAppPolicyCache _runtimeCache;
        private readonly IUmbracoContextFactory _context;

        public VirtualNodesContentFinder(AppCaches appCaches, IUmbracoContextFactory context, ILogger<VirtualNodesContentFinder> logger)
        {
            _runtimeCache = appCaches.RuntimeCache;
            _context = context;
        }
        public bool TryFindContent(IPublishedRequestBuilder contentRequest)
        {
            var _umbracoContext       = _context.EnsureUmbracoContext().UmbracoContext;
            var cachedVirtualNodeUrls = _runtimeCache.GetCacheItem<Dictionary<string, int>>("CachedVirtualNodes");
            var path                  = contentRequest.Uri.AbsolutePath;

            // If found in the cached dictionary
            if ((cachedVirtualNodeUrls != null) && cachedVirtualNodeUrls.ContainsKey(path))
            {
                var nodeId = cachedVirtualNodeUrls[path];

                contentRequest.SetPublishedContent(_umbracoContext.Content.GetById(nodeId));

                return true;
            }

            // If not found in the cached dictionary, traverse nodes and find the node that corresponds to the URL
            var rootNodes = _umbracoContext.Content.GetAtRoot();
            var items = rootNodes.DescendantsOrSelf<IPublishedContent>();
            var item = rootNodes.DescendantsOrSelf<IPublishedContent>().Where(x => {
                var uri = new Uri(x.Url());
                return uri.AbsolutePath == (path + "/") || uri.AbsolutePath == path;
            }).FirstOrDefault();

            // If item is found, return it after adding it to the cache so we don't have to go through the same process again.
            if (cachedVirtualNodeUrls == null)
            {
                cachedVirtualNodeUrls = new Dictionary<string, int>();
            }

            // If we have found a node that corresponds to the URL given
            if (item != null)
            {
                // Update cache
                _runtimeCache.InsertCacheItem("CachedVirtualNodes", () => cachedVirtualNodeUrls, null, false);

                // That's all folks
                contentRequest.SetPublishedContent(item);

                return true;
            }

            return false;
        }
    }
}
