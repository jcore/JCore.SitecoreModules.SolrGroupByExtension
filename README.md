JCore.SitecoreModules.SolrGroupByExtension
==========================================

Sitecore 7 GroupBy extension for Solr.

Example:

<pre>
using (var context = objIndexName.GetContext(item))
{
var query = GenerateQuery(searchCriteria, context, item);
var group = query.GroupResults(context, g => g.TemplateId, numberofItemsPerGroupForRelatedWidgets);
totalResultCount = group.TotalSearchResults;
globalSearchResult.Add(group);
}
</pre>
