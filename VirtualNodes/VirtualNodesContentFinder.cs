using System.Collections.Generic;
using System.Linq;
using System.Web.Caching;
using Umbraco.Core.Cache;
using Umbraco.Core.Composing;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace VirtualNodes
{
    public class VirtualNodesContentFinder : IContentFinder
    {
        public bool TryFindContent(PublishedRequest contentRequest)
        {
            var _runtimeCache         = Current.AppCaches.RuntimeCache;
            var _umbracoContext       = contentRequest.UmbracoContext;
            var cachedVirtualNodeUrls = _runtimeCache.GetCacheItem<Dictionary<string, int>>("CachedVirtualNodes");
            var path                  = contentRequest.Uri.AbsolutePath;

            // If found in the cached dictionary
            if ((cachedVirtualNodeUrls != null) && cachedVirtualNodeUrls.ContainsKey(path))
            {
                var nodeId = cachedVirtualNodeUrls[path];

                contentRequest.PublishedContent = _umbracoContext.Content.GetById(nodeId);

                return true;
            }

            // If not found in the cached dictionary, traverse nodes and find the node that corresponds to the URL
            var rootNodes             = _umbracoContext.Content.GetAtRoot();
            var item                  = rootNodes.DescendantsOrSelf<IPublishedContent>().Where(x => (x.Url == (path + "/") || (x.Url == path))).FirstOrDefault();

            // If item is found, return it after adding it to the cache so we don't have to go through the same process again.
            if (cachedVirtualNodeUrls == null)
            {
                cachedVirtualNodeUrls = new Dictionary<string, int>();
            }

            // If we have found a node that corresponds to the URL given
            if (item != null)
            {
                // Update cache
                cachedVirtualNodeUrls.Add(path, item.Id); 
                _runtimeCache.InsertCacheItem("CachedVirtualNodes", () => cachedVirtualNodeUrls, null, false, CacheItemPriority.High);

                // That's all folks
                contentRequest.PublishedContent = item;

                return true;
            }

            return false;
        }
    }
}
