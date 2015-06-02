using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.ContentSearch.Linq;
using SolrNet;
using SolrNet.Impl;

namespace JCore.SitecoreModules.SolrSearchExtensions.Search.Solr
{
    public class ExtendedSearchResults<TSource>
    {
        public string CorrectedSpelling { get; set; }
        public IDictionary<string, IList<TSource>> SimilarResults { get; set; }
        public int TotalSearchResults { get; private set; }
        public IEnumerable<SearchHit<TSource>> Hits { get; private set; }
        public IEnumerable<Linq.GroupedResults<TSource>> Groups { get; private set; }
        public FacetResults Facets { get; private set; }
        public IDictionary<string, HighlightedSnippets> Highlights { get; private set; }

        public ExtendedSearchResults(IEnumerable<SearchHit<TSource>> results, int totalSearchResults)
        {
            if (results == null)
                throw new ArgumentNullException("results");
            this.Hits = results;
            this.TotalSearchResults = totalSearchResults;
        }

        public ExtendedSearchResults(IEnumerable<SearchHit<TSource>> results, int totalSearchResults, FacetResults facets = null)
            : this(results, totalSearchResults)
        {
            this.Facets = facets;
        }

        public ExtendedSearchResults(IEnumerable<Linq.GroupedResults<TSource>> results, int totalSearchResults, FacetResults facets = null)
            : this(results, totalSearchResults)
        {
            this.Facets = facets;
        }

        public ExtendedSearchResults(IEnumerable<SearchHit<TSource>> results, IEnumerable<Linq.GroupedResults<TSource>> groups, int totalSearchResults, string spellcheckedString, FacetResults facets = null)
            : this(results, totalSearchResults)
        {
            this.Facets = facets;
            this.Groups = groups;
            this.CorrectedSpelling = spellcheckedString;
        }

        public ExtendedSearchResults(IEnumerable<SearchHit<TSource>> results, IEnumerable<Linq.GroupedResults<TSource>> groups, int totalSearchResults, string spellcheckedString, IDictionary<string, HighlightedSnippets> highlights, FacetResults facets = null)
            : this(results, totalSearchResults)
        {
            this.Facets = facets;
            this.Groups = groups;
            this.CorrectedSpelling = spellcheckedString;
            this.Highlights = highlights;
        }

        public ExtendedSearchResults(IEnumerable<Linq.GroupedResults<TSource>> results, int totalSearchResults)
        {
            if (results == null)
                throw new ArgumentNullException("results");
            this.Groups = results;
            this.TotalSearchResults = totalSearchResults;
        }

    }
}



