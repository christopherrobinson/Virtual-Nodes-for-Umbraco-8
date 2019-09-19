using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using Umbraco.Core.Configuration;
using Umbraco.Core.Configuration.UmbracoSettings;
using Umbraco.Core.Logging;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Routing;

namespace VirtualNodes
{
    public class VirtualNodesUrlProvider : DefaultUrlProvider
    {
        private readonly IRequestHandlerSection _requestSettings;

        public VirtualNodesUrlProvider(IRequestHandlerSection requestSettings, ILogger logger, IGlobalSettings globalSettings, ISiteDomainHelper siteDomainHelper)
            : base(requestSettings, logger, globalSettings, siteDomainHelper)
        {
            _requestSettings = requestSettings;
        }

        public override IEnumerable<UrlInfo> GetOtherUrls(UmbracoContext umbracoContext, int id, Uri current)
        {
            return base.GetOtherUrls(umbracoContext, id, current);
        }

        public override UrlInfo GetUrl(UmbracoContext umbracoContext, IPublishedContent content, UrlMode mode, string culture, Uri current)
        {
            // If this is a virtual node itself, no need to handle it - should return normal URL
            var hasVirtualNodeInPath = false;

            foreach (var item in content.Ancestors())
            {
                if (item.IsVirtualNode())
                {
                    hasVirtualNodeInPath = true;

                    break;
                }
            }

            return (hasVirtualNodeInPath ? ConstructUrl(umbracoContext, content, mode, culture, current) : base.GetUrl(umbracoContext, content, mode, culture, current));
        }


        private UrlInfo ConstructUrl(UmbracoContext umbracoContext, IPublishedContent content, UrlMode mode, string culture, Uri current)
        {
            string path = content.Path;

            // Keep path items in par with path segments in url
            // If we are hiding the top node from path, then we'll have to skip one path item (the root). 
            // If we are not, then we'll have to skip two path items (root and home)
            var hideTopNode = ConfigurationManager.AppSettings.Get("Umbraco.Core.HideTopLevelNodeFromPath");

            if (String.IsNullOrEmpty(hideTopNode))
            {
                hideTopNode = "false";
            }

            var pathItemsToSkip = ((hideTopNode == "true") ? 2 : 1);

            // Get the path ids but skip what's needed in order to have the same number of elements in url and path ids
            var pathIds = path.Split(',').Skip(pathItemsToSkip).Reverse().ToArray();

            // Get the default url 
            // DO NOT USE THIS - RECURSES: string url = content.Url;
            // https://our.umbraco.org/forum/developers/extending-umbraco/73533-custom-url-provider-stackoverflowerror
            // https://our.umbraco.org/forum/developers/extending-umbraco/66741-iurlprovider-cannot-evaluate-expression-because-the-current-thread-is-in-a-stack-overflow-state
            UrlInfo url = base.GetUrl(umbracoContext, content, mode, culture, current);
            var urlText = url == null ? "" : url.Text;

            // If we come from an absolute URL, strip the host part and keep it so that we can append
            // it again when returing the URL. 
            var hostPart = "";

            if (urlText.StartsWith("http"))
            {
                var uri = new Uri(urlText);

                urlText = urlText.Replace(uri.GetLeftPart(UriPartial.Authority), "");
                hostPart = uri.GetLeftPart(UriPartial.Authority);
            }

            // Strip leading and trailing slashes 
            if (urlText.EndsWith("/"))
            {
                urlText = urlText.Substring(0, urlText.Length - 1);
            }

            if (urlText.StartsWith("/"))
            {
                urlText = urlText.Substring(1, urlText.Length - 1);
            }

            // Now split the url. We should have as many elements as those in pathIds.
            string[] urlParts = urlText.Split('/').Reverse().ToArray();

            // Iterate the url parts. Check the corresponding path id and if the document that corresponds there
            // is of a type that must be excluded from the path, just make that url part an empty string.
            var i = 0;

            foreach (var urlPart in urlParts)
            {
                var currentItem = umbracoContext.Content.GetById(int.Parse(pathIds[i]));

                // Omit any virtual node unless it's leaf level (we still need this otherwise it will be pointing to parent's URL)
                if (currentItem.IsVirtualNode() && i > 0)
                {
                    urlParts[i] = "";
                }

                i++;
            }

            // Reconstruct the url, leaving out all parts that we emptied above. This 
            // will be our final url, without the parts that correspond to excluded nodes.
            string finalUrl = String.Join("/", urlParts.Reverse().Where(x => x != "").ToArray());

            // Just in case - check if there are trailing and leading slashes and add them if not
            if (!finalUrl.EndsWith("/") && _requestSettings.AddTrailingSlash)
            {
                finalUrl += "/";
            }

            if (!finalUrl.StartsWith("/"))
            {
                finalUrl = "/" + finalUrl;
            }

            finalUrl = String.Concat(hostPart, finalUrl);

            // Voila
            return new UrlInfo(finalUrl, true, culture);
        }
    }
}
