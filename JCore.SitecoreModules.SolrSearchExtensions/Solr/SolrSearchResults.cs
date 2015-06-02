using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.Pipelines.IndexingFilters;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.SolrProvider;
using JCore.SitecoreModules.SolrSearchExtensions.Search.Solr.Linq;
using SolrNet;
using System.Text.RegularExpressions;
using SolrNet.Impl;
using Sitecore.ContentSearch.Abstractions;

namespace JCore.SitecoreModules.SolrSearchExtensions.Search.Solr
{
    public struct SolrSearchResults<TElement>
    {
        private readonly SolrSearchContext context;
        private readonly SolrQueryResults<Dictionary<string, object>> searchResults;
        private readonly IDictionary<string, SolrNet.GroupedResults<Dictionary<string, object>>> groupedSearchResults;
        private readonly SolrIndexConfiguration solrIndexConfiguration;
        private readonly IIndexDocumentPropertyMapper<Dictionary<string, object>> mapper;
        private readonly SelectMethod selectMethod;
        private readonly IEnumerable<IFieldQueryTranslator> virtualFieldProcessors;
        private readonly int numberFound;
        private readonly string spellCheckerResults;
        private readonly IDictionary<string, HighlightedSnippets> highlights;
        private readonly IEnumerable<IExecutionContext> executionContexts;

        public int NumberFound
        {
            get
            {
                return this.numberFound;
            }
        }

        public SolrSearchResults(SolrSearchContext context, SolrQueryResults<Dictionary<string, object>> searchResults, SelectMethod selectMethod, IEnumerable<IExecutionContext> executionContexts, IEnumerable<IFieldQueryTranslator> virtualFieldProcessors)
        {
            this.context = context;
            this.solrIndexConfiguration = (SolrIndexConfiguration)this.context.Index.Configuration;
            this.executionContexts = executionContexts;

            OverrideExecutionContext<IIndexDocumentPropertyMapper<Dictionary<string, object>>> executionContext = this.executionContexts != null ? Enumerable.FirstOrDefault<IExecutionContext>(this.executionContexts, (Func<IExecutionContext, bool>)(c => c is OverrideExecutionContext<IIndexDocumentPropertyMapper<Dictionary<string, object>>>)) as OverrideExecutionContext<IIndexDocumentPropertyMapper<Dictionary<string, object>>> : (OverrideExecutionContext<IIndexDocumentPropertyMapper<Dictionary<string, object>>>)null;
            this.mapper = (executionContext != null ? executionContext.OverrideObject : (IIndexDocumentPropertyMapper<Dictionary<string, object>>)null) ?? this.solrIndexConfiguration.IndexDocumentPropertyMapper;

            this.selectMethod = selectMethod;
            this.virtualFieldProcessors = virtualFieldProcessors;
            this.numberFound = searchResults.NumFound;
            this.searchResults = SolrSearchResults<TElement>.ApplySecurity(searchResults, context.SecurityOptions, context.Index.Locator.GetInstance<ICorePipeline>(), context.Index.Locator.GetInstance<IAccessRight>(), ref this.numberFound);
            this.groupedSearchResults = SolrSearchResults<TElement>.ApplyGroupSecurity(this.searchResults.Grouping, context.SecurityOptions, context.Index.Locator.GetInstance<ICorePipeline>(), context.Index.Locator.GetInstance<IAccessRight>(), ref this.numberFound);
            this.spellCheckerResults = SolrSearchResults<TElement>.GetSpellCheckedString(searchResults.SpellChecking);
            this.highlights = searchResults.Highlights;
        }

        /// <summary>
        /// Gets the spell checked string.
        /// </summary>
        /// <param name="collection">The collection.</param>
        /// <returns></returns>
        private static string GetSpellCheckedString(SpellCheckResults results)
        {
            if (!string.IsNullOrEmpty(results.Collation) && results.Count > 0 && results.FirstOrDefault(r => r.Suggestions.Any() && r.Suggestions.Any(s => s != "true")) != null)
            {
                return results.Collation;
            }
            return string.Empty;
        }

        private static IDictionary<string, SolrNet.GroupedResults<Dictionary<string, object>>> ApplyGroupSecurity(IDictionary<string, SolrNet.GroupedResults<Dictionary<string, object>>> solrQueryResults, SearchSecurityOptions options, ICorePipeline pipeline, IAccessRight accessRight, ref int numberFound)
        {
            if (!options.HasFlag((Enum)SearchSecurityOptions.DisableSecurityCheck))
            {
                foreach (var grouped in solrQueryResults)
                {
                    numberFound += grouped.Value.Matches;
                    foreach (var groups in grouped.Value.Groups)
                    {
                        var groupCount = groups.NumFound;
                        var groupResults = groups.Documents;
                        HashSet<Dictionary<string, object>> hashSet = new HashSet<Dictionary<string, object>>();
                        foreach (Dictionary<string, object> dictionary in Enumerable.Where<Dictionary<string, object>>((IEnumerable<Dictionary<string, object>>)groupResults, (Func<Dictionary<string, object>, bool>)(searchResult => searchResult != null)))
                        {
                            object obj1;
                            if (dictionary.TryGetValue("_uniqueid", out obj1))
                            {
                                object obj2;
                                dictionary.TryGetValue("_datasource", out obj2);
                                if (OutboundIndexFilterPipeline.CheckItemSecurity(pipeline, accessRight, new OutboundIndexFilterArgs((string)obj1, (string)obj2)))
                                {
                                    hashSet.Add(dictionary);
                                    numberFound = numberFound - 1;
                                    groupCount--;
                                }
                            }
                        }
                        groups.NumFound = groupCount;
                        foreach (Dictionary<string, object> dictionary in hashSet)
                            groupResults.Remove(dictionary);
                    }
                }
            }
            return solrQueryResults;
        }

        private static SolrQueryResults<Dictionary<string, object>> ApplySecurity(SolrQueryResults<Dictionary<string, object>> solrQueryResults, SearchSecurityOptions options, ICorePipeline pipeline, IAccessRight accessRight, ref int numberFound)
        {
            if (!options.HasFlag((Enum)SearchSecurityOptions.DisableSecurityCheck))
            {
                HashSet<Dictionary<string, object>> hashSet = new HashSet<Dictionary<string, object>>();
                foreach (Dictionary<string, object> dictionary in Enumerable.Where<Dictionary<string, object>>((IEnumerable<Dictionary<string, object>>)solrQueryResults, (Func<Dictionary<string, object>, bool>)(searchResult => searchResult != null)))
                {
                    object obj1;
                    if (dictionary.TryGetValue("_uniqueid", out obj1))
                    {
                        object obj2;
                        dictionary.TryGetValue("_datasource", out obj2);
                        if (OutboundIndexFilterPipeline.CheckItemSecurity(pipeline, accessRight, new OutboundIndexFilterArgs((string)obj1, (string)obj2)))
                        {
                            hashSet.Add(dictionary);
                            numberFound = numberFound - 1;
                        }
                    }
                }
                foreach (Dictionary<string, object> dictionary in hashSet)
                    solrQueryResults.Remove(dictionary);
            }
            return solrQueryResults;
        }


        public TElement ElementAt(int index)
        {
            if (index < 0 || index > this.searchResults.Count)
                throw new IndexOutOfRangeException();
            else
                return this.mapper.MapToType<TElement>(this.searchResults[index], this.selectMethod, this.virtualFieldProcessors, this.context.SecurityOptions);
        }

        public TElement ElementAtOrDefault(int index)
        {
            if (index < 0 || index > this.searchResults.Count)
                return default(TElement);
            else
                return this.mapper.MapToType<TElement>(this.searchResults[index], this.selectMethod, this.virtualFieldProcessors, this.context.SecurityOptions);
        }

        public bool Any()
        {
            return this.numberFound > 0;
        }

        public long Count()
        {
            return (long)this.numberFound;
        }

        public TElement First()
        {
            if (this.searchResults.Count < 1)
                throw new InvalidOperationException("Sequence contains no elements");
            else
                return this.ElementAt(0);
        }

        public TElement FirstOrDefault()
        {
            if (this.searchResults.Count < 1)
                return default(TElement);
            else
                return this.ElementAt(0);
        }

        public TElement Last()
        {
            if (this.searchResults.Count < 1)
                throw new InvalidOperationException("Sequence contains no elements");
            else
                return this.ElementAt(this.searchResults.Count - 1);
        }

        public TElement LastOrDefault()
        {
            if (this.searchResults.Count < 1)
                return default(TElement);
            else
                return this.ElementAt(this.searchResults.Count - 1);
        }

        public TElement Single()
        {
            if (this.Count() < 1L)
                throw new InvalidOperationException("Sequence contains no elements");
            if (this.Count() > 1L)
                throw new InvalidOperationException("Sequence contains more than one element");
            else
                return this.mapper.MapToType<TElement>(this.searchResults[0], this.selectMethod, this.virtualFieldProcessors, this.context.SecurityOptions);
        }

        public TElement SingleOrDefault()
        {
            if (this.Count() != 1L)
                return default(TElement);
            else
                return this.mapper.MapToType<TElement>(this.searchResults[0], this.selectMethod, this.virtualFieldProcessors, this.context.SecurityOptions);
        }

        public IEnumerable<SearchHit<TElement>> GetSearchHits()
        {
            foreach (Dictionary<string, object> document in (List<Dictionary<string, object>>)this.searchResults)
            {
                float score = -1f;
                object scoreObj;
                if (document.TryGetValue("score", out scoreObj) && scoreObj is float)
                    score = (float)scoreObj;
                yield return new SearchHit<TElement>(score, this.mapper.MapToType<TElement>(document, this.selectMethod, this.virtualFieldProcessors, this.context.SecurityOptions));
            }
        }

        public IEnumerable<TElement> GetSearchResults()
        {
            foreach (Dictionary<string, object> document in (List<Dictionary<string, object>>)this.searchResults)
                yield return this.mapper.MapToType<TElement>(document, this.selectMethod, this.virtualFieldProcessors, this.context.SecurityOptions);
        }

        public Dictionary<string, ICollection<KeyValuePair<string, int>>> GetFacets()
        {
            IDictionary<string, ICollection<KeyValuePair<string, int>>> facetFields = this.searchResults.FacetFields;
            IDictionary<string, IList<Pivot>> facetPivots = this.searchResults.FacetPivots;
            Dictionary<string, ICollection<KeyValuePair<string, int>>> dictionary = Enumerable.ToDictionary<KeyValuePair<string, ICollection<KeyValuePair<string, int>>>, string, ICollection<KeyValuePair<string, int>>>((IEnumerable<KeyValuePair<string, ICollection<KeyValuePair<string, int>>>>)facetFields, (Func<KeyValuePair<string, ICollection<KeyValuePair<string, int>>>, string>)(x => x.Key), (Func<KeyValuePair<string, ICollection<KeyValuePair<string, int>>>, ICollection<KeyValuePair<string, int>>>)(x => x.Value));
            if (facetPivots.Any())
            {
                foreach (KeyValuePair<string, IList<Pivot>> keyValuePair in (IEnumerable<KeyValuePair<string, IList<Pivot>>>)facetPivots)
                    dictionary[keyValuePair.Key] = this.Flatten((IEnumerable<Pivot>)keyValuePair.Value, string.Empty);
            }
            IDictionary<string, DateFacetingResult> facetDates = this.searchResults.FacetDates;
            if (facetDates.Any())
            {
                foreach (KeyValuePair<string, DateFacetingResult> keyValuePair in (IEnumerable<KeyValuePair<string, DateFacetingResult>>)facetDates)
                    dictionary[keyValuePair.Key] = this.Flatten((DateFacetingResult)keyValuePair.Value);
            }
            return dictionary;
        }

        public IEnumerable<JCore.SitecoreModules.SolrSearchExtensions.Search.Solr.Linq.GroupedResults<TElement>> GetGroupedResults()
        {
            var groupedResults = new List<JCore.SitecoreModules.SolrSearchExtensions.Search.Solr.Linq.GroupedResults<TElement>>();
            foreach (var group in groupedSearchResults)
            {
                groupedResults.Add(this.ProcessGroupResult(group.Value));
            }
            return groupedResults;
        }

        private Linq.GroupedResults<TElement> ProcessGroupResult(SolrNet.GroupedResults<Dictionary<string, object>> group)
        {
            var result = new Linq.GroupedResults<TElement>()
            {
                Groups = this.ProcessGroups(group.Groups),
                Ngroups = group.Ngroups,
                Matches = group.Matches
            };
            return result;
        }

        private IEnumerable<Linq.Group<TElement>> ProcessGroups(ICollection<SolrNet.Group<Dictionary<string, object>>> collection)
        {
            foreach (var group in collection)
            {
                yield return this.ProcessGroup(group);
            }
        }

        private Linq.Group<TElement> ProcessGroup(SolrNet.Group<Dictionary<string, object>> group)
        {
            var groupResult = new Linq.Group<TElement>()
            {
                Documents = this.ProcessDocuments(group.Documents),
                GroupValue = group.GroupValue,
                NumFound = group.NumFound
            };
            return groupResult;
        }

        private IEnumerable<TElement> ProcessDocuments(ICollection<Dictionary<string, object>> collection)
        {
            foreach (var document in collection)
            {
                yield return this.mapper.MapToType<TElement>(document, this.selectMethod, this.virtualFieldProcessors, this.context.SecurityOptions);
            }
        }

        private ICollection<KeyValuePair<string, int>> Flatten(IEnumerable<Pivot> pivots, string parentName)
        {
            HashSet<KeyValuePair<string, int>> hashSet = new HashSet<KeyValuePair<string, int>>();
            foreach (Pivot pivot in pivots)
            {
                if (parentName != string.Empty)
                    hashSet.Add(new KeyValuePair<string, int>(parentName + "/" + pivot.Value, pivot.Count));
                if (pivot.HasChildPivots)
                    hashSet.UnionWith((IEnumerable<KeyValuePair<string, int>>)this.Flatten((IEnumerable<Pivot>)pivot.ChildPivots, pivot.Value));
            }
            return (ICollection<KeyValuePair<string, int>>)hashSet;
        }

        private ICollection<KeyValuePair<string, int>> Flatten(DateFacetingResult dates)
        {
            HashSet<KeyValuePair<string, int>> hashSet = new HashSet<KeyValuePair<string, int>>();
            foreach (var dateResult in dates.DateResults.OrderByDescending(d => d.Key))
            {
                hashSet.Add(new KeyValuePair<string, int>(dateResult.Key.ToString("MMMM, yyyy"), dateResult.Value));
            }
            return (ICollection<KeyValuePair<string, int>>)hashSet;
        }

        public string GetSpellCheckedResults()
        {
            return this.spellCheckerResults;
        }

        /// <summary>
        /// Gets the highlights.
        /// </summary>
        /// <value>
        /// The highlights.
        /// </value>
        public IDictionary<string, HighlightedSnippets> Highlights
        {
            get
            {
                return this.highlights;
            }
        }

    }
}
