using System.Collections.Generic;
using System.Text;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.SolrProvider;
using SolrNet;
using Microsoft.Practices.ServiceLocation;
using SolrNet.Commands.Parameters;
using SolrNet.Impl;
using Sitecore.Data.Items;
using Sitecore.ContentSearch.Pipelines.GetContextIndex;
using Sitecore.ContentSearch.SearchTypes;

namespace JCore.SitecoreModules.SolrSearchExtensions.Search.Solr
{
    public class SpellCheck : SearchProvider
    {
        ISolrOperations<SearchResultItem> solr;
        private string indexName = string.Empty;
        
        /// <summary>
        /// Get teh current instance of the SOLR
        /// </summary>
        /// <param name="item">Get the item state</param>
        public SpellCheck(Item item)
        {
            solr = ServiceLocator.Current.GetInstance<ISolrOperations<SearchResultItem>>();
            var indexable = new SitecoreIndexableItem(item);
            indexName = GetContextIndexName(indexable);
        }

        /// <summary>
        /// Get the context index name 
        /// </summary>
        /// <param name="indexable">Indexable item</param>
        /// <returns></returns>
        public override sealed string GetContextIndexName(IIndexable indexable)
        {
            var objContextIndexArgs = new GetContextIndexArgs(indexable);
            string context = GetContextIndexPipeline.Run(objContextIndexArgs);
            return context;
        }

        /// <summary>
        /// Checks the spelling.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public string CheckSpelling(string text, out bool spellingCorrected)
        {
            var results = solr.Query(text, new QueryOptions
            {
                SpellCheck = new SpellCheckingParameters { Collate = true },
                FilterQueries = new ISolrQuery[] { new SolrQueryByField("_indexname", indexName) },
                Rows = 0
            });
            spellingCorrected = false;
            if (results.SpellChecking != null && results.SpellChecking.Collation != null)
            {
                spellingCorrected = true;
                return results.SpellChecking.Collation;
            }
            return text;
        }
    }
}
