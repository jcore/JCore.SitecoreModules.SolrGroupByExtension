using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.ContentSearch.Linq;
using SolrNet;

namespace JCore.SitecoreModules.SolrSearchExtensions.Search.Solr
{
    public class ExtendedSearchResults<TSource>
    {
        public string SpellCheckedResponse { get; set; }
        public IDictionary<string, IList<TSource>> SimilarResults { get; set; }
        public int TotalSearchResults { get; private set; }
        public IEnumerable<SearchHit<TSource>> Hits { get; private set; }
        public IEnumerable<Linq.GroupedResults<TSource>> Groups { get; private set; }
        public FacetResults Facets { get; private set; }


        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedSearchResults{TSource}"/> class.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <param name="totalSearchResults">The total search results.</param>
        /// <exception cref="System.ArgumentNullException">results</exception>
        public ExtendedSearchResults(IEnumerable<SearchHit<TSource>> results, int totalSearchResults)
        {
            if (results == null)
                throw new ArgumentNullException("results");
            this.Hits = results;
            this.TotalSearchResults = totalSearchResults;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedSearchResults{TSource}"/> class.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <param name="totalSearchResults">The total search results.</param>
        /// <param name="facets">The facets.</param>
        public ExtendedSearchResults(IEnumerable<SearchHit<TSource>> results, int totalSearchResults, FacetResults facets = null)
            : this(results, totalSearchResults)
        {
            this.Facets = facets;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedSearchResults{TSource}"/> class.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <param name="totalSearchResults">The total search results.</param>
        /// <param name="facets">The facets.</param>
        public ExtendedSearchResults(IEnumerable<Linq.GroupedResults<TSource>> results, int totalSearchResults, FacetResults facets = null)
            : this(results, totalSearchResults)
        {
            this.Facets = facets;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ExtendedSearchResults{TSource}"/> class.
        /// </summary>
        /// <param name="results">The results.</param>
        /// <param name="totalSearchResults">The total search results.</param>
        /// <exception cref="System.ArgumentNullException">results</exception>
        public ExtendedSearchResults(IEnumerable<Linq.GroupedResults<TSource>> results, int totalSearchResults)
        {
            if (results == null)
                throw new ArgumentNullException("results");
            this.Groups = results;
            this.TotalSearchResults = totalSearchResults;
        }

    }
}



