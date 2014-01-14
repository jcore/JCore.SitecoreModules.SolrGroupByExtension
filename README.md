JCore.SitecoreModules.SolrGroupByExtension
==========================================

Sitecore 7 GroupBy extension for Solr. This is not a true Sitecore Extension. Unfortunately current version of Sitecore (7.0) doesn't allow extending Solr provider, so a lot of files in this library had to be copied from Sitecore assembly and changed to implement GroupBy functionality.

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
