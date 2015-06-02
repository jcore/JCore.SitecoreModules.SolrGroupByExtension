using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Solr;
using SolrNet;
using SolrNet.Commands.Parameters;

namespace JCore.SitecoreModules.SolrSearchExtensions.Search.Solr
{
    public class ExtendedCompositeQuery : SolrCompositeQuery
    {
        public QueryOptions QueryOptions { get; set; }
        public LocalParams LocalParams { get; set; }
        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedCompositeQuery"/> class.
        /// </summary>
        /// <param name="query">The query.</param>
        /// <param name="filterQuery">The filter query.</param>
        /// <param name="methods">The methods.</param>
        /// <param name="virtualFieldProcessors">The virtual field processors.</param>
        /// <param name="facetQueries">The facet queries.</param>
        /// <param name="options">The options.</param>
        /// <param name="localParams">The local parameters.</param>
        public ExtendedCompositeQuery(AbstractSolrQuery query, AbstractSolrQuery filterQuery, IEnumerable<Sitecore.ContentSearch.Linq.Methods.QueryMethod> methods, IEnumerable<IFieldQueryTranslator> virtualFieldProcessors, IEnumerable<FacetQuery> facetQueries, QueryOptions options, LocalParams localParams = null)
            : base(query, filterQuery, methods, virtualFieldProcessors, facetQueries)
        {
            QueryOptions = options;
            LocalParams = localParams;
        }
    }
}
