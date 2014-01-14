using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JCore.SitecoreModules.SolrSearchExtensions.Search.Solr.Linq
{
    /// <summary>
    /// 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class GroupedResults<T>
    {
        public int Matches { get; set; }

        public IEnumerable<Group<T>> Groups { get; set; }

        public int? Ngroups { get; set; }

        public GroupedResults()
        {
            this.Groups = (IEnumerable<Group<T>>)new List<Group<T>>();
        }
    }
}
