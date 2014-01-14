using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using JCore.SitecoreModules.SolrSearchExtensions.Search;
using JCore.SitecoreModules.SolrSearchExtensions.Search.Solr;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Utilities;
using Sitecore.ContentSearch.SearchTypes;
using Sitecore.ContentSearch.Security;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace JCore.SitecoreModules.SolrSearchExtensions.Managers
{
    public class SearchManager
    {
        private const int numberofItemsPerGroupForRelatedWidgets = 10;

        /// <summary>
        /// Groupeds the search.
        /// </summary>
        /// <typeparam name="TItem">The type of the item.</typeparam>
        /// <param name="searchCriteria">The search criteria.</param>
        /// <param name="totalResultCount">The total result count.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public static IEnumerable<ExtendedSearchResults<TItem>> GroupedSearch<TItem>(IBaseCriteria searchCriteria, out int totalResultCount, Item item) where TItem : SearchResultItem, new()
        {
            var globalSearchResult = new List<ExtendedSearchResults<TItem>>();

            var objIndexName = ContentSearchManager.GetIndex(Config.IndexName);

            if (item == null && !string.IsNullOrEmpty(searchCriteria.ItemId))
            {
                item = Sitecore.Context.Database.GetItem(searchCriteria.ItemId);
            }

            using (var context = objIndexName.CreateSearchContext(SearchSecurityOptions.EnableSecurityCheck))
            {
                var query = GenerateQuery<TItem>(searchCriteria, context, item);
                var group = query.GroupResults(context, g => g.TemplateId, numberofItemsPerGroupForRelatedWidgets);
                totalResultCount = group.TotalSearchResults;
                globalSearchResult.Add(group);
            }

            return globalSearchResult;
        }

        /// <summary>
        /// Generates the query.
        /// </summary>
        /// <param name="searchCriteria">The search criteria.</param>
        /// <param name="context">The context.</param>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        /// <exception cref="System.NotImplementedException"></exception>
        private static IQueryable<TItem> GenerateQuery<TItem>(IBaseCriteria searchCriteria, Sitecore.ContentSearch.IProviderSearchContext context, Item item) where TItem : SearchResultItem, new()
        {
            var query = context.GetQueryable<TItem>();

            // applying filters (fq parameter)
            if (searchCriteria.Filters != null && searchCriteria.Filters.Any())
            {
                foreach (var param in searchCriteria.Filters.Keys)
                {
                    var paramValues = searchCriteria.Filters[param];
                    var predicate = PredicateBuilder.True<TItem>();
                    var separatedParameters = param.Split(new char[] { ',', '|' });
                    foreach (var val in paramValues)
                    {
                        foreach (var par in separatedParameters)
                        {
                            predicate = predicate.Or(p => p[(ObjectIndexerKey)par] == GetObjectValue(val));
                        }
                    }
                    query = query.Filter(predicate);
                }
            }
            // exclude templates. (for Other section in search results) JG.
            if (searchCriteria.ExcludeFilters != null && searchCriteria.ExcludeFilters.Any())
            {
                foreach (var param in searchCriteria.ExcludeFilters.Keys)
                {
                    var paramValues = searchCriteria.ExcludeFilters[param];
                    var predicate = PredicateBuilder.True<TItem>();
                    var separatedParameters = param.Split(new char[] { ',', '|' });
                    foreach (var val in paramValues)
                    {
                        foreach (var par in separatedParameters)
                        {
                            predicate = predicate.Or(p => p[(ObjectIndexerKey)par] == GetObjectValue(val));
                        }
                    }
                    query = query.Filter(predicate);
                }
            }

            if (searchCriteria.PageSize > 0)
            {
                query = query.Page(searchCriteria.PageNumber, searchCriteria.PageSize);
            }

            return query;
        }

        /// <summary>
        /// Gets the object value.
        /// </summary>
        /// <param name="val">The value.</param>
        /// <returns></returns>
        public static object GetObjectValue(object val)
        {
            if (val is string)
            {
                var strVal = (string)val;
                if (string.IsNullOrEmpty(strVal))
                {
                    return val;
                }
                ID id = ID.Null;
                if (ID.TryParse(val, out id))
                {
                    return id;
                }
                ShortID shortId;
                if (ShortID.TryParse(strVal, out shortId))
                {
                    return shortId;
                }
                DateTime date;
                if (DateTime.TryParse(strVal, out date))
                {
                    return date;
                }
                return strVal.ToLowerInvariant();
            }
            else
            {
                return val;
            }
        }
    }
}
