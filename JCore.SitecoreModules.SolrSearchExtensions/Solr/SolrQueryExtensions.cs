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
using Sitecore.ContentSearch.Linq.Nodes;
using Sitecore.Configuration;
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
        public static ExtendedSearchResults<TSource> GroupResults<TSource, TKey>(this IQueryable<TSource> source, IProviderSearchContext context, Expression<Func<TSource, TKey>> keySelector, int groupLimit, bool includSpellChecking = false, Operator op = Operator.AND, string text = "", IDictionary<BoostField, string> boostFields = null)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var linqToSolr = new CustomLinqToSolrIndex<TSource>((SolrSearchContext)context, (IExecutionContext)null);

            var param = "_template";
            MemberExpression member = keySelector.Body as MemberExpression;
            if (member != null)
            {
                PropertyInfo propInfo = member.Member as PropertyInfo;
                if (propInfo != null)
                {
                    var expressionParser = new ExpressionParser(typeof(TKey), typeof(TSource), linqToSolr.PublicFieldNameTranslator);
                    var methodInfo = expressionParser.GetType().GetMethod("Visit", BindingFlags.Instance | BindingFlags.NonPublic, Type.DefaultBinder, new Type[] { typeof(Expression) }, null);
                    if (methodInfo != null)
                    {
                        object classInstance = Activator.CreateInstance(typeof(TSource), null);
                        QueryNode queryNode = methodInfo.Invoke(expressionParser, new object[] { keySelector.Body }) as QueryNode;
                        FieldNode fieldNode = queryNode as FieldNode;
                        if (fieldNode != null)
                        {
                            param = fieldNode.FieldKey;
                        }
                    }
                }
            }
            var extendedQuery = ExtendNativeQuery((IHasNativeQuery)source, groupLimit, param, includSpellChecking, op, text, boostFields);
            return linqToSolr.Execute<ExtendedSearchResults<TSource>>(extendedQuery);
        }

        /// <summary>
        /// Facets the on date.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <typeparam name="TKey">The type of the key.</typeparam>
        /// <param name="source">The source.</param>
        /// <param name="context">The context.</param>
        /// <param name="keySelector">The key selector.</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentNullException">source</exception>
        public static ExtendedSearchResults<TSource> GetExtendedResults<TSource>(this IQueryable<TSource> source, IProviderSearchContext context, bool includSpellChecking, Operator op, string text, bool includeDateFacets, IDictionary<BoostField, string> boostFields = null)
        {
            if (source == null)
                throw new ArgumentNullException("source");

            var linqToSolr = new CustomLinqToSolrIndex<TSource>((SolrSearchContext)context, (IExecutionContext)null);
            var extendedQuery = ExtendNativeQuery((IHasNativeQuery)source, includSpellChecking, op, text, includeDateFacets, boostFields);
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
                        Query = text,
                        Count = 5
                    },
                    Rows = 0
                });

            var linqToSolr = new CustomLinqToSolrIndex<TSource>((SolrSearchContext)context, (IExecutionContext)null);
            return linqToSolr.Execute<string>(newQuery);
        }

        /// <summary>
        /// Autosuggests the specified query.
        /// </summary>
        /// <typeparam name="TSource">The type of the source.</typeparam>
        /// <param name="query">The query.</param>
        /// <param name="context">The context.</param>
        /// <param name="text">The text.</param>
        /// <returns></returns>
        //public static SuggestResult<TSource> Autosuggest<TSource>(this IQueryable<TSource> query, IProviderSearchContext context, string text)
        //{
        //    var extendedQuery = (SolrCompositeQuery)((IHasNativeQuery)query).Query;
        //    extendedQuery.Methods.Add((QueryMethod)new GetResultsMethod(GetResultsOptions.Default));

        //    var newQuery = new ExtendedCompositeQuery(extendedQuery.Query, extendedQuery.Filter, extendedQuery.Methods, extendedQuery.VirtualFieldProcessors, extendedQuery.FacetQueries,
        //        new QueryOptions()
        //        {
        //            ExtraParams = new Dictionary<string, string> { { "qt", "suggest" } },
        //            SpellCheck = new SpellCheckingParameters()
        //            {
        //                Collate = true,
        //                Query = text
        //            },
        //            Rows = 0
        //        });

        //    var linqToSolr = new CustomLinqToSolrIndex<TSource>((SolrSearchContext)context, (IExecutionContext)null);
        //    return linqToSolr.Execute<SuggestResult<TSource>>(newQuery);
        //}

        /// <summary>
        /// Extends the native query.
        /// </summary>
        /// <param name="hasNativeQuery">The has native query.</param>
        /// <returns></returns>
        private static ExtendedCompositeQuery ExtendNativeQuery(IHasNativeQuery hasNativeQuery, int groupLimit, string expression, bool includSpellChecking, Operator op, string text, IDictionary<BoostField, string> boostFields = null)
        {
            var query = (SolrCompositeQuery)hasNativeQuery.Query;
            query.Methods.Add((QueryMethod)new GetResultsMethod(GetResultsOptions.Default));

            var options = new QueryOptions()
            {
                Grouping = new GroupingParameters()
                {
                    Fields = new[] { expression },
                    Format = GroupingFormat.Grouped,
                    Limit = groupLimit,
                },
                Highlight = GetHighlightParameter()
            };

            if (includSpellChecking && !string.IsNullOrWhiteSpace(text))
            {
                options.SpellCheck = new SpellCheckingParameters()
                {
                    Collate = true,
                    Query = text,
                    Count = 5
                };
            }
            else
            {
                options.SpellCheck = new SpellCheckingParameters()
                {
                    Collate = false,
                    Count = 0
                };
            }

            var extraParams = new Dictionary<string, string>();
            var localParams = new LocalParams();

            if (boostFields != null)
            {
                foreach (var field in boostFields.Keys)
                {
                    switch (field)
                    {
                        case BoostField.DateLatestFirst:
                            localParams.Add("boost b", string.Format("recip(ms(NOW,{0}),3.16e-11,1,1)", boostFields[field]));
                            break;
                        default:
                            break;
                    }

                }
                options.ExtraParams = extraParams;
            }
            if (op == Operator.AND)
            {
                localParams.Add("q.op", "AND");
                extraParams.Add("q.op", "AND");
            }
            return new ExtendedCompositeQuery(query.Query, query.Filter, query.Methods, query.VirtualFieldProcessors, query.FacetQueries,
                    options, localParams);
        }

        /// <summary>
        /// Gets the highlight parameter.
        /// </summary>
        /// <returns></returns>
        private static HighlightingParameters GetHighlightParameter()
        {
            var snippetCount = Config.SolrHighlightsNumberOfSnippets;
            var fields = Config.SolrHighlightsFields;
            var regexString = Config.SolrHighlightsRegexPattern;
            var regexFragsize = Config.SolrHighlightsFragsize;
            var regexSlop = Config.SolrHighlightsRegexSlop;
            return new HighlightingParameters
            {
                Fields = fields,
                Fragmenter = SolrHighlightFragmenter.Regex,
                RegexPattern = regexString,
                Fragsize = regexFragsize,
                RegexSlop = regexSlop
            };
        }

        /// <summary>
        /// Extends the native query.
        /// </summary>
        /// <param name="hasNativeQuery">The has native query.</param>
        /// <returns></returns>
        private static ExtendedCompositeQuery ExtendNativeQuery(IHasNativeQuery hasNativeQuery, bool includSpellChecking, Operator op, string text, bool includeDateFacets, IDictionary<BoostField, string> boostFields = null)
        {
            var query = (SolrCompositeQuery)hasNativeQuery.Query;
            query.Methods.Add((QueryMethod)new GetResultsMethod(GetResultsOptions.Default));

            var options = new QueryOptions()
            {
                Highlight = GetHighlightParameter()
            };

            if (includeDateFacets)
            {
                var endDate = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(1);
                var startDate = endDate.AddYears(-20);
                var queries = ((SolrMultipleCriteriaQuery)query.Query).Queries;
                if (queries.Any())
                {
                    foreach (SolrMultipleCriteriaQuery subQ in queries.Where(q => q is SolrMultipleCriteriaQuery))
                    {
                        if (subQ.Queries.Any())
                        {
                            var rangeQueries = subQ.Queries.Where(q => q is SolrQueryByRange<string>);
                            if (rangeQueries.Any())
                            {
                                var dateRangeQuery = (SolrQueryByRange<string>)rangeQueries.FirstOrDefault(q => ((SolrQueryByRange<string>)q).FieldName.Contains("date"));
                                if (dateRangeQuery != null)
                                {
                                    var fromDate = DateTime.Parse(dateRangeQuery.From);
                                    startDate = new DateTime(fromDate.Year, fromDate.Month, 1).AddMonths(1);
                                    var toDate = DateTime.Parse(dateRangeQuery.To);
                                    endDate = new DateTime(toDate.Year, toDate.Month, 1).AddMonths(1);
                                }
                            }
                        }
                    }
                }
                options.Facet = new FacetParameters
                {
                    Queries = new[] { 
                        new SolrFacetDateQuery(
                            Config.DateField, 
                            startDate /* range start */, 
                            endDate /* range end */, 
                            "+1MONTH" /* gap */)
                    },
                    MinCount = 1
                };
            }

            if (includSpellChecking && !string.IsNullOrWhiteSpace(text))
            {
                options.SpellCheck = new SpellCheckingParameters()
                {
                    Collate = true,
                    Query = text,
                    Count = 5
                };
            }
            else
            {
                options.SpellCheck = new SpellCheckingParameters()
                {
                    Collate = false,
                    Count = 0
                };
            }

            var extraParams = new Dictionary<string, string>();
            var localParams = new LocalParams();

            if (boostFields != null)
            {
                foreach (var field in boostFields.Keys)
                {
                    switch (field)
                    {
                        case BoostField.DateLatestFirst:
                            localParams.Add("boost b", string.Format("recip(ms(NOW,{0}),3.16e-11,1,1)", boostFields[field]));
                            break;
                        default:
                            break;
                    }

                }
                options.ExtraParams = extraParams;
            }
            if (op == Operator.AND)
            {
                localParams.Add("q.op", "AND");
                extraParams.Add("q.op", "AND");
            }
            return new ExtendedCompositeQuery(query.Query, query.Filter, query.Methods, query.VirtualFieldProcessors, query.FacetQueries,
                    options, localParams);
        }
    }

    public enum Operator
    {
        OR, AND
    }

    public enum BoostField
    {
        DateLatestFirst
    }

}
