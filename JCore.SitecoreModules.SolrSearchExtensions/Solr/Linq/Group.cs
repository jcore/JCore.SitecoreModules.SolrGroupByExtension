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
    public class Group<T>
    {
        public string GroupValue { get; set; }

        public int NumFound { get; set; }

        public IEnumerable<T> Documents { get; set; }

        public Group()
        {
            this.Documents = (IEnumerable<T>)new List<T>();
        }
    }
}
