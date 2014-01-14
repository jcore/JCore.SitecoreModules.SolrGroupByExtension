using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.Linq.Parsing;
using Sitecore.ContentSearch.Linq.Solr;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.Data;
using SolrNet;
using SolrNet.Commands.Parameters;
using SolrNet.DSL;
//using JCore.SitecoreModules.SolrSearchExtensions.GroupedSearch.Solr.Linq.Methods;

namespace JCore.SitecoreModules.SolrSearchExtensions.Search.Solr
{
    public static class SolrQueryExtensions
    {
        /// <summary>
        /// Groups the results.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="context">The context.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        public static ExtendedSearchResults<TSource> GroupResults<TSource, TKey>(this IQueryable<TSource> source, IProviderSearchContext context, Expression<Func<TSource, TKey>> keySelector, int groupLimit)
        {
            if (source == null)
                throw new ArgumentNullException("source");
            var extendedQuery = ExtendNativeQuery((IHasNativeQuery)source, groupLimit, "_template");
            var linqToSolr = new CustomLinqToSolrIndex<TSource>((SolrSearchContext)context, (IExecutionContext)null);
            return linqToSolr.Execute<ExtendedSearchResults<TSource>>(extendedQuery);            
        }

        /// <summary>
        /// Checks the spelling.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="context">The context.</param>
        /// <returns></returns>
        public static string CheckSpelling<TSource>(this IQueryable<TSource> query, IProviderSearchContext context, string text)
        {
            var extendedQuery = (SolrCompositeQuery)((IHasNativeQuery)query).Query;
            extendedQuery.Methods.Add((QueryMethod)new GetResultsMethod(GetResultsOptions.Default));            

            var newQuery = new ExtendedCompositeQuery(extendedQuery.Query, extendedQuery.Filter, extendedQuery.Methods, extendedQuery.VirtualFieldProcessors, extendedQuery.FacetQueries,
                new QueryOptions()
                {
                    SpellCheck = new SpellCheckingParameters()
                    {
                        Collate = true,
                        Query = text
                    },
                    Rows = 0
                });

            var linqToSolr = new CustomLinqToSolrIndex<TSource>((SolrSearchContext)context, (IExecutionContext)null);
            return linqToSolr.Execute<string>(newQuery);
        }

        /// <summary>
        /// Extends the native query.
        /// </summary>
        /// <param name="hasNativeQuery">The has native query.</param>
        /// <returns></returns>
        private static ExtendedCompositeQuery ExtendNativeQuery(IHasNativeQuery hasNativeQuery, int groupLimit, string expression)
        {            
            var query = (SolrCompositeQuery)hasNativeQuery.Query;
            query.Methods.Add((QueryMethod)new GetResultsMethod(GetResultsOptions.Default));
            //query.Methods.Add((QueryMethod)new GroupByMethod("_template", typeof(ID)));

            var extendedQuery = new ExtendedCompositeQuery(query.Query, query.Filter, query.Methods, query.VirtualFieldProcessors, query.FacetQueries,
                new QueryOptions()
                {
                    Grouping = new GroupingParameters()
                    {
                        Fields = new[] { expression },
                        Format = GroupingFormat.Grouped,
                        Limit = groupLimit,
                    },
                    SpellCheck = new SpellCheckingParameters()
                    {
                        Collate = true 
                    }
                });

            return extendedQuery;
        }
    }
}
