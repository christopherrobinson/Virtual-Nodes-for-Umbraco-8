using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using Umbraco.Cms.Core.Configuration.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Routing;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace VirtualNodes
{
    public class VirtualNodesUrlProvider : DefaultUrlProvider
    {
        private readonly RequestHandlerSettings _requestSettings;
        private readonly IUmbracoContextAccessor _umbracoContextAccessor;
        private readonly IConfiguration _configuration;

        public VirtualNodesUrlProvider(IOptions<RequestHandlerSettings> requestSettings, ILogger<DefaultUrlProvider> logger, ISiteDomainMapper siteDomainMapper, IUmbracoContextAccessor umbracoContextAccessor, UriUtility uriUtility, IConfiguration configuration)
            : base(requestSettings, logger, siteDomainMapper, umbracoContextAccessor, uriUtility)
        {
            _requestSettings = requestSettings.Value;
            _umbracoContextAccessor = umbracoContextAccessor;
            _configuration = configuration;
        }

        public override IEnumerable<UrlInfo> GetOtherUrls(int id, Uri current)
        {
            return base.GetOtherUrls(id, current);
        }

        public override UrlInfo GetUrl(IPublishedContent content, UrlMode mode, string culture, Uri current)
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

            return (hasVirtualNodeInPath ? ConstructUrl(content, mode, culture, current) : base.GetUrl(content, mode, culture, current));
        }


        private UrlInfo ConstructUrl(IPublishedContent content, UrlMode mode, string culture, Uri current)
        {
            string path = content.Path;

            // Keep path items in par with path segments in url
            // If we are hiding the top node from path, then we'll have to skip one path item (the root). 
            // If we are not, then we'll have to skip two path items (root and home)
            // var hideTopNode = ConfigurationManager.AppSettings.Get("Umbraco.Core.HideTopLevelNodeFromPath");
            // Changed to the correct setting in the appsettings.json
            var hideTopNode = _configuration["Umbraco:CMS:Global:HideTopLevelNodeFromPath"];

            if (string.IsNullOrEmpty(hideTopNode))
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
            UrlInfo url = base.GetUrl(content, mode, culture, current);
            var urlText = url == null ? "" : url.Text;

            // If we come from an absolute URL, strip the host part and keep it so that we can append
            // it again when returning the URL.
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
            // - Unless the top-level node provided a folder-style hostname
            string[] urlParts = urlText.Split('/').Reverse().ToArray();
            var hasHostnameFolder = urlParts.Length > pathIds.Length;
            if (hasHostnameFolder)
            {
                //recalc the pathIds
                pathIds = path.Split(',').Skip(pathItemsToSkip-1).Reverse().ToArray();
            }

            // Iterate the url parts. Check the corresponding path id and if the document that corresponds there
            // is of a type that must be excluded from the path, just make that url part an empty string.
            var i = 0;

            foreach (var urlPart in urlParts)
            {
                var currentItem = _umbracoContextAccessor.GetRequiredUmbracoContext().Content.GetById(int.Parse(pathIds[i]));

                // Omit any virtual node unless it's leaf level (we still need this otherwise it will be pointing to parent's URL)
                if (currentItem != null && currentItem.IsVirtualNode() && i > 0)
                {
                    urlParts[i] = "";
                }

                i++;
            }

            // Reconstruct the url, leaving out all parts that we emptied above. This 
            // will be our final url, without the parts that correspond to excluded nodes.
            string finalUrl = string.Join("/", urlParts.Reverse().Where(x => x != "").ToArray());

            // Just in case - check if there are trailing and leading slashes and add them if not
            if (!finalUrl.EndsWith("/") && _requestSettings.AddTrailingSlash)
            {
                finalUrl += "/";
            }

            if (!finalUrl.StartsWith("/"))
            {
                finalUrl = "/" + finalUrl;
            }

            finalUrl = string.Concat(hostPart, finalUrl);
            // Voila
            return new UrlInfo(finalUrl, true, culture);
        }
    }
}
