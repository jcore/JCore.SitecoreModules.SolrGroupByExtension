using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using Microsoft.Practices.ServiceLocation;
using Sitecore.Configuration;
using Sitecore.ContentSearch;
using Sitecore.ContentSearch.Diagnostics;
using Sitecore.ContentSearch.Linq;
using Sitecore.ContentSearch.Linq.Common;
using Sitecore.ContentSearch.Linq.Methods;
using Sitecore.ContentSearch.Linq.Nodes;
using Sitecore.ContentSearch.Linq.Parsing;
using Sitecore.ContentSearch.Linq.Solr;
using Sitecore.ContentSearch.Pipelines.GetFacets;
using Sitecore.ContentSearch.Pipelines.ProcessFacets;
using Sitecore.ContentSearch.Security;
using Sitecore.ContentSearch.SolrProvider;
using Sitecore.ContentSearch.SolrProvider.Logging;
using Sitecore.ContentSearch.Utilities;
using Sitecore.Diagnostics;
using SolrNet;
using SolrNet.Commands.Parameters;
using SolrNet.Exceptions;


namespace JCore.SitecoreModules.SolrSearchExtensions.Search.Solr
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="TItem">The type of the item.</typeparam>
    public class CustomLinqToSolrIndex<TItem> : LinqToSolrIndex<TItem>
    {
        private readonly SolrSearchContext context;
        private readonly string cultureCode;
        private IContentSearchConfigurationSettings contentSearchSettings1;
        public FieldNameTranslator PublicFieldNameTranslator { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomLinqToSolrIndex{TItem}"/> class.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="executionContext">The execution context.</param>
        public CustomLinqToSolrIndex(SolrSearchContext context, IExecutionContext executionContext)
            : base(context, executionContext)
        {
            Assert.ArgumentNotNull((object)context, "context");
            this.context = context;
            this.contentSearchSettings1 = context.Index.Locator.GetInstance<IContentSearchConfigurationSettings>();
            CultureExecutionContext executionContext1 = this.Parameters.ExecutionContext as CultureExecutionContext;
            CultureInfo culture = executionContext1 == null ? CultureInfo.GetCultureInfo(Settings.DefaultLanguage) : executionContext1.Culture;
            this.cultureCode = culture.TwoLetterISOLanguageName;
            ((SolrFieldNameTranslator)this.Parameters.FieldNameTranslator).AddCultureContext(culture);
            this.PublicFieldNameTranslator = this.FieldNameTranslator;
        }

        /// <summary>
        /// Executes the specified composite query.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <param name="compositeQuery">The composite query.</param>
        /// <returns></returns>
        public TResult Execute<TResult>(ExtendedCompositeQuery compositeQuery)
        {
            if (typeof(TResult).IsGenericType && typeof(TResult).GetGenericTypeDefinition() == typeof(ExtendedSearchResults<>))
            {

                Type resultType = typeof(TResult).GetGenericArguments()[0];
                SolrQueryResults<Dictionary<string, object>> solrQueryResults = this.Execute(compositeQuery, resultType);
                Type type = typeof(SolrSearchResults<>).MakeGenericType(new Type[1]
                    {
                      resultType
                    });
                MethodInfo methodInfo = this.GetType().GetMethod("GetExtendedResults", BindingFlags.Instance | BindingFlags.NonPublic).MakeGenericMethod(typeof(TResult), resultType);
                SelectMethod selectMethod = CustomLinqToSolrIndex<TItem>.GetSelectMethod(compositeQuery);
                object instance = Activator.CreateInstance(type, new object[5]
                {
                  (object) this.context,
                  (object) solrQueryResults,
                  (object) selectMethod,
                  (IEnumerable<IExecutionContext>) compositeQuery.ExecutionContexts,
                  (object) compositeQuery.VirtualFieldProcessors
                });
                return (TResult)methodInfo.Invoke(this, new object[] { compositeQuery, instance, solrQueryResults });
            }
            else
            {
                return base.Execute<TResult>(compositeQuery);
            }
        }

        /// <summary>
        /// Gets the extended results.
        /// </summary>
        /// <typeparam name="TResult">The type of the result.</typeparam>
        /// <typeparam name="TDocument">The type of the document.</typeparam>
        /// <param name="compositeQuery">The composite query.</param>
        /// <param name="processedResults">The processed results.</param>
        /// <param name="results">The results.</param>
        /// <returns></returns>
        internal TResult GetExtendedResults<TResult, TDocument>(ExtendedCompositeQuery compositeQuery, SolrSearchResults<TDocument> processedResults, SolrQueryResults<Dictionary<string, object>> results)
        {
            object obj = default(TResult);
            IEnumerable<Linq.GroupedResults<TDocument>> groups = processedResults.GetGroupedResults();
            FacetResults facetResults = this.FormatFacetResults(processedResults.GetFacets(), compositeQuery.FacetQueries);
            IEnumerable<SearchHit<TDocument>> searchResults = processedResults.GetSearchHits();
            var spellcheckedResponse = processedResults.GetSpellCheckedResults();
            obj = Activator.CreateInstance(typeof(TResult), (object)searchResults, (object)groups, (object)processedResults.NumberFound, spellcheckedResponse, processedResults.Highlights, (object)facetResults);
            return (TResult)Convert.ChangeType(obj, typeof(TResult));
        }

        /// <summary>
        /// Executes the specified composite query.
        /// </summary>
        /// <param name="compositeQuery">The composite query.</param>
        /// <param name="resultType">Type of the result.</param>
        /// <returns></returns>
        internal SolrQueryResults<Dictionary<string, object>> Execute(ExtendedCompositeQuery compositeQuery, Type resultType)
        {
            QueryOptions options = compositeQuery.QueryOptions;
            if (compositeQuery.Methods != null)
            {
                List<SelectMethod> list1 = Enumerable.ToList<SelectMethod>(Enumerable.Select<QueryMethod, SelectMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.Select)), (Func<QueryMethod, SelectMethod>)(m => (SelectMethod)m)));
                if (Enumerable.Any<SelectMethod>((IEnumerable<SelectMethod>)list1))
                {
                    foreach (string str in Enumerable.SelectMany<SelectMethod, string>((IEnumerable<SelectMethod>)list1, (Func<SelectMethod, IEnumerable<string>>)(selectMethod => (IEnumerable<string>)selectMethod.FieldNames)))
                        options.Fields.Add(str.ToLowerInvariant());
                    if (!this.context.SecurityOptions.HasFlag((Enum)SearchSecurityOptions.DisableSecurityCheck))
                    {
                        options.Fields.Add("_uniqueid");
                        options.Fields.Add("_datasource");
                    }
                }
                List<GetResultsMethod> list2 = Enumerable.ToList<GetResultsMethod>(Enumerable.Select<QueryMethod, GetResultsMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.GetResults)), (Func<QueryMethod, GetResultsMethod>)(m => (GetResultsMethod)m)));
                if (Enumerable.Any<GetResultsMethod>((IEnumerable<GetResultsMethod>)list2))
                {
                    if (options.Fields.Count > 0)
                    {
                        options.Fields.Add("score");
                    }
                    else
                    {
                        options.Fields.Add("*");
                        options.Fields.Add("score");
                    }
                }
                List<OrderByMethod> list3 = Enumerable.ToList<OrderByMethod>(Enumerable.Select<QueryMethod, OrderByMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.OrderBy)), (Func<QueryMethod, OrderByMethod>)(m => (OrderByMethod)m)));
                if (Enumerable.Any<OrderByMethod>((IEnumerable<OrderByMethod>)list3))
                {
                    foreach (OrderByMethod orderByMethod in list3)
                    {
                        string field = orderByMethod.Field;
                        options.AddOrder(new SortOrder[1]
            {
              new SortOrder(field, orderByMethod.SortDirection == SortDirection.Ascending ? Order.ASC : Order.DESC)
            });
                    }
                }
                List<SkipMethod> list4 = Enumerable.ToList<SkipMethod>(Enumerable.Select<QueryMethod, SkipMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.Skip)), (Func<QueryMethod, SkipMethod>)(m => (SkipMethod)m)));
                if (Enumerable.Any<SkipMethod>((IEnumerable<SkipMethod>)list4))
                {
                    int num = Enumerable.Sum<SkipMethod>((IEnumerable<SkipMethod>)list4, (Func<SkipMethod, int>)(skipMethod => skipMethod.Count));
                    options.Start = new int?(num);
                }
                List<TakeMethod> list5 = Enumerable.ToList<TakeMethod>(Enumerable.Select<QueryMethod, TakeMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.Take)), (Func<QueryMethod, TakeMethod>)(m => (TakeMethod)m)));
                if (Enumerable.Any<TakeMethod>((IEnumerable<TakeMethod>)list5))
                {
                    int num = Enumerable.Sum<TakeMethod>((IEnumerable<TakeMethod>)list5, (Func<TakeMethod, int>)(takeMethod => takeMethod.Count));
                    options.Rows = new int?(num);
                }
                List<CountMethod> list6 = Enumerable.ToList<CountMethod>(Enumerable.Select<QueryMethod, CountMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.Count)), (Func<QueryMethod, CountMethod>)(m => (CountMethod)m)));
                if (compositeQuery.Methods.Count == 1 && Enumerable.Any<CountMethod>((IEnumerable<CountMethod>)list6))
                    options.Rows = new int?(0);
                List<AnyMethod> list7 = Enumerable.ToList<AnyMethod>(Enumerable.Select<QueryMethod, AnyMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.Any)), (Func<QueryMethod, AnyMethod>)(m => (AnyMethod)m)));
                if (compositeQuery.Methods.Count == 1 && Enumerable.Any<AnyMethod>((IEnumerable<AnyMethod>)list7))
                    options.Rows = new int?(0);
                List<GetFacetsMethod> list8 = Enumerable.ToList<GetFacetsMethod>(Enumerable.Select<QueryMethod, GetFacetsMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.GetFacets)), (Func<QueryMethod, GetFacetsMethod>)(m => (GetFacetsMethod)m)));
                if (compositeQuery.FacetQueries.Count > 0 && (Enumerable.Any<GetFacetsMethod>((IEnumerable<GetFacetsMethod>)list8) || Enumerable.Any<GetResultsMethod>((IEnumerable<GetResultsMethod>)list2)))
                {
                    foreach (FacetQuery facetQuery in EnumerableExtensions.ToHashSet<FacetQuery>(GetFacetsPipeline.Run(new GetFacetsArgs((IQueryable)null, (IEnumerable<FacetQuery>)compositeQuery.FacetQueries, (IDictionary<string, IVirtualFieldProcessor>)this.context.Index.Configuration.VirtualFieldProcessors, (FieldNameTranslator)this.context.Index.FieldNameTranslator)).FacetQueries))
                    {
                        if (Enumerable.Any<string>(facetQuery.FieldNames))
                        {
                            int? minimumResultCount = facetQuery.MinimumResultCount;
                            if (Enumerable.Count<string>(facetQuery.FieldNames) == 1)
                            {
                                SolrFieldNameTranslator fieldNameTranslator = this.FieldNameTranslator as SolrFieldNameTranslator;
                                string str = Enumerable.First<string>(facetQuery.FieldNames);
                                if (fieldNameTranslator != null && str == fieldNameTranslator.StripKnownExtensions(str) && this.context.Index.Configuration.FieldMap.GetFieldConfiguration(str) == null)
                                    str = fieldNameTranslator.GetIndexFieldName(str.Replace("__", "!").Replace("_", " ").Replace("!", "__"), true);
                                QueryOptions queryOptions = options;
                                ISolrFacetQuery[] solrFacetQueryArray1 = new ISolrFacetQuery[1];
                                solrFacetQueryArray1[0] = (ISolrFacetQuery)new SolrFacetFieldQuery(str)
                                {
                                    MinCount = minimumResultCount
                                };
                                ISolrFacetQuery[] solrFacetQueryArray2 = solrFacetQueryArray1;
                                queryOptions.AddFacets(solrFacetQueryArray2);
                            }
                            if (Enumerable.Count<string>(facetQuery.FieldNames) > 1)
                            {
                                QueryOptions queryOptions = options;
                                ISolrFacetQuery[] solrFacetQueryArray1 = new ISolrFacetQuery[1];
                                solrFacetQueryArray1[0] = (ISolrFacetQuery)new SolrFacetPivotQuery()
                                {
                                    Fields = (ICollection<string>)new string[1]
                  {
                    string.Join(",", facetQuery.FieldNames)
                  },
                                    MinCount = minimumResultCount
                                };
                                ISolrFacetQuery[] solrFacetQueryArray2 = solrFacetQueryArray1;
                                queryOptions.AddFacets(solrFacetQueryArray2);
                            }
                        }
                    }
                    if (!Enumerable.Any<GetResultsMethod>((IEnumerable<GetResultsMethod>)list2))
                        options.Rows = new int?(0);
                }

                //List<GroupByMethod> list9 = Enumerable.ToList<GroupByMethod>(Enumerable.Select<QueryMethod, GroupByMethod>(compositeQuery.Methods.Where(m => m.GetType() == typeof(GroupByMethod)), (Func<QueryMethod, GroupByMethod>)(m => (GroupByMethod)m)));
                //if (list9.Any())
                //{
                //    foreach (GroupByMethod groupByMethod in list9)
                //    {
                //        options.Grouping = new GroupingParameters()
                //        {
                //            Fields = new[] { groupByMethod.Field },
                //            Format = GroupingFormat.Grouped,
                //            Limit = 20,
                //        };
                //    }
                //    if (options.Fields.Count > 0)
                //    {
                //        options.Fields.Add("score");
                //    }
                //    else
                //    {
                //        options.Fields.Add("*");
                //        options.Fields.Add("score");
                //    }
                //}
                //List<CheckSpellingMethod> list10 = Enumerable.ToList<CheckSpellingMethod>(Enumerable.Select<QueryMethod, CheckSpellingMethod>(compositeQuery.Methods.Where(m => m.GetType() == typeof(CheckSpellingMethod)), (Func<QueryMethod, CheckSpellingMethod>)(m => (CheckSpellingMethod)m)));
                //if (list10.Any())
                //{
                //    foreach (CheckSpellingMethod groupByMethod in list10)
                //    {
                //        options.SpellCheck = new SpellCheckingParameters()
                //        {
                //            Collate = true
                //        };
                //    }
                //}
            }
            if (compositeQuery.Filter != null)
                options.AddFilterQueries(new ISolrQuery[1]
                {
                  (ISolrQuery) compositeQuery.Filter
                });
            options.AddFilterQueries(new ISolrQuery[1]
                  {
                    (ISolrQuery) new SolrQueryByField("_indexname", this.context.Index.Name)
                  });
            if (!Settings.DefaultLanguage.StartsWith(this.cultureCode))
            {
                QueryOptions queryOptions = options;
                ISolrQuery[] solrQueryArray1 = new ISolrQuery[1];
                solrQueryArray1[0] = (ISolrQuery)new SolrQueryByField("_language", this.cultureCode + "*")
                {
                    Quoted = false
                };
                ISolrQuery[] solrQueryArray2 = solrQueryArray1;
                queryOptions.AddFilterQueries(solrQueryArray2);
            }
            SolrLoggingSerializer loggingSerializer = new SolrLoggingSerializer();
            string q = loggingSerializer.SerializeQuery((ISolrQuery)compositeQuery.Query);

            SolrSearchIndex solrSearchIndex = this.context.Index as SolrSearchIndex;
            try
            {
                if (!options.Rows.HasValue)
                    options.Rows = new int?(this.contentSearchSettings1.SearchMaxResults());
                SearchLog.Log.Info("Query - " + q, (Exception)null);
                var solrOperations = ServiceLocator.Current.GetInstance<ISolrOperations<Dictionary<string, object>>>(solrSearchIndex.Core);

                if (compositeQuery.LocalParams != null)
                {
                    SearchLog.Log.Info("Serialized Query - ?q=" + compositeQuery.LocalParams.ToString() + q + "&" + string.Join("&", Enumerable.ToArray<string>(Enumerable.Select<KeyValuePair<string, string>, string>(loggingSerializer.GetAllParameters(options), (Func<KeyValuePair<string, string>, string>)(p => string.Format("{0}={1}", (object)p.Key, (object)p.Value))))), (Exception)null);
                    return solrOperations.Query(compositeQuery.LocalParams + q, options);
                }
                SearchLog.Log.Info("Serialized Query - ?q=" + q + "&" + string.Join("&", Enumerable.ToArray<string>(Enumerable.Select<KeyValuePair<string, string>, string>(loggingSerializer.GetAllParameters(options), (Func<KeyValuePair<string, string>, string>)(p => string.Format("{0}={1}", (object)p.Key, (object)p.Value))))), (Exception)null);
                return solrOperations.Query(q, options);
            }
            catch (Exception ex)
            {
                if (!(ex is SolrConnectionException) && !(ex is SolrNetException))
                {
                    throw;
                }
                else
                {
                    string message = ex.Message;
                    if (ex.Message.StartsWith("<?xml"))
                    {
                        XmlDocument xmlDocument = new XmlDocument();
                        xmlDocument.LoadXml(ex.Message);
                        XmlNode xmlNode1 = xmlDocument.SelectSingleNode("/response/lst[@name='error'][1]/str[@name='msg'][1]");
                        XmlNode xmlNode2 = xmlDocument.SelectSingleNode("/response/lst[@name='responseHeader'][1]/lst[@name='params'][1]/str[@name='q'][1]");
                        if (xmlNode1 != null && xmlNode2 != null)
                        {
                            SearchLog.Log.Error(string.Format("Solr Error : [\"{0}\"] - Query attempted: [{1}]", (object)xmlNode1.InnerText, (object)xmlNode2.InnerText), (Exception)null);
                            return new SolrQueryResults<Dictionary<string, object>>();
                        }
                    }
                    Log.Error(message, (object)this);
                    return new SolrQueryResults<Dictionary<string, object>>();
                }
            }
        }

        private FacetResults FormatFacetResults(Dictionary<string, ICollection<KeyValuePair<string, int>>> facetResults, List<FacetQuery> facetQueries)
        {
            SolrFieldNameTranslator fieldNameTranslator = this.context.Index.FieldNameTranslator as SolrFieldNameTranslator;
            IDictionary<string, ICollection<KeyValuePair<string, int>>> dictionary = ProcessFacetsPipeline.Run(new ProcessFacetsArgs(facetResults, (IEnumerable<FacetQuery>)facetQueries, (IEnumerable<FacetQuery>)facetQueries, (IDictionary<string, IVirtualFieldProcessor>)this.context.Index.Configuration.VirtualFieldProcessors, (FieldNameTranslator)fieldNameTranslator));
            foreach (FacetQuery facetQuery in facetQueries)
            {
                FacetQuery originalQuery = facetQuery;
                if (originalQuery.FilterValues != null && Enumerable.Any<object>(originalQuery.FilterValues) && dictionary.ContainsKey(originalQuery.CategoryName))
                {
                    ICollection<KeyValuePair<string, int>> collection = dictionary[originalQuery.CategoryName];
                    dictionary[originalQuery.CategoryName] = (ICollection<KeyValuePair<string, int>>)Enumerable.ToList<KeyValuePair<string, int>>(Enumerable.Where<KeyValuePair<string, int>>((IEnumerable<KeyValuePair<string, int>>)collection, (Func<KeyValuePair<string, int>, bool>)(cv => Enumerable.Contains<object>(originalQuery.FilterValues, (object)cv.Key))));
                }
            }
            FacetResults facetResults1 = new FacetResults();
            foreach (KeyValuePair<string, ICollection<KeyValuePair<string, int>>> keyValuePair in (IEnumerable<KeyValuePair<string, ICollection<KeyValuePair<string, int>>>>)dictionary)
            {
                if (fieldNameTranslator != null)
                {
                    string key = keyValuePair.Key;
                    string name;
                    if (key.Contains(","))
                        name = fieldNameTranslator.StripKnownExtensions((IEnumerable<string>)key.Split(new char[1]
            {
              ','
            }, StringSplitOptions.RemoveEmptyEntries));
                    else
                        name = fieldNameTranslator.StripKnownExtensions(key);
                    IEnumerable<FacetValue> values = Enumerable.Select<KeyValuePair<string, int>, FacetValue>((IEnumerable<KeyValuePair<string, int>>)keyValuePair.Value, (Func<KeyValuePair<string, int>, FacetValue>)(v => new FacetValue(v.Key, v.Value)));
                    facetResults1.Categories.Add(new FacetCategory(name, values));
                }
            }
            return facetResults1;
        }
        private static SelectMethod GetSelectMethod(SolrCompositeQuery compositeQuery)
        {
            List<SelectMethod> list = Enumerable.ToList<SelectMethod>(Enumerable.Select<QueryMethod, SelectMethod>(Enumerable.Where<QueryMethod>((IEnumerable<QueryMethod>)compositeQuery.Methods, (Func<QueryMethod, bool>)(m => m.MethodType == QueryMethodType.Select)), (Func<QueryMethod, SelectMethod>)(m => (SelectMethod)m)));
            if (Enumerable.Count<SelectMethod>((IEnumerable<SelectMethod>)list) != 1)
                return (SelectMethod)null;
            else
                return list[0];
        }
    }
}
