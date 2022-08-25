using Umbraco.Cms.Core.Composing;
using Umbraco.Cms.Core.DependencyInjection;

namespace VirtualNodes
{
    public class VirtualNodesComposer : IComposer
    {

        public void Compose(IUmbracoBuilder builder)
        {
            builder.ContentFinders().Insert<VirtualNodesContentFinder>();
            builder.UrlProviders().Insert<VirtualNodesUrlProvider>();
        }
    }
}
