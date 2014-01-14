using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCore.SitecoreModules.SolrSearchExtensions.Search
{
    public interface IBaseCriteria
    {
        List<string> BaseTemplateIds { get; set; }
        Dictionary<string, IEnumerable<object>> ExcludeFilters { get; set; }
        Dictionary<string, IEnumerable<object>> Filters { get; set; }
        string ItemId { get; set; }
        int PageNumber { get; set; }
        int PageSize { get; set; }
        string TemplateId { get; set; }
    }
}
