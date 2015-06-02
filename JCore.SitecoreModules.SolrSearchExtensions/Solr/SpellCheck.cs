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
using Sitecore.ContentSearch.SolrProvider.Logging;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Abstractions;
using System.Linq;
using System;

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
        public override string GetContextIndexName(IIndexable indexable)
        {
            var objContextIndexArgs = new GetContextIndexArgs(indexable);
            return GetContextIndexPipeline.Run(objContextIndexArgs);
        }

        public override string GetContextIndexName(IIndexable indexable, ICorePipeline pipeline)
        {
            var objContextIndexArgs = new GetContextIndexArgs(indexable);
            return GetContextIndexPipeline.Run(pipeline, objContextIndexArgs);
        }
        /// <summary>
        /// Checks the spelling.
        /// </summary>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        public string CheckSpelling(string text, out bool spellingCorrected)
        {
            var options = new QueryOptions
            {
                SpellCheck = new SpellCheckingParameters { Collate = true, OnlyMorePopular = true },
                FilterQueries = new ISolrQuery[] { new SolrQueryByField("_indexname", indexName) },
                Rows = 0
            };
            var results = solr.Query(text, options);
            spellingCorrected = false;
            SolrLoggingSerializer loggingSerializer = new SolrLoggingSerializer();
            SearchLog.Log.Info("Serialized Query Spellcheck - ?q=" + text + "&" + string.Join("&", Enumerable.ToArray<string>(Enumerable.Select<KeyValuePair<string, string>, string>(loggingSerializer.GetAllParameters(options), (Func<KeyValuePair<string, string>, string>)(p => string.Format("{0}={1}", (object)p.Key, (object)p.Value))))), (Exception)null);

            if (results.SpellChecking != null && results.SpellChecking.Collation != null)
            {
                spellingCorrected = true;
                SearchLog.Log.Info("Serialized Query Spellcheck result - " + results.SpellChecking.Collation);
                return results.SpellChecking.Collation;
            }
            return text;
        }
    }
}
