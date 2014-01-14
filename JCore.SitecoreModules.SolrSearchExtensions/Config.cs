using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sitecore.Configuration;

namespace JCore.SitecoreModules.SolrSearchExtensions
{
    public static class Config
    {
        /// <summary>
        /// Gets the name of the index.
        /// </summary>
        /// <value>
        /// The name of the index.
        /// Default value is master. If we dont have any value associated with a key named as "WeilWWW.IndexName" in Sitcore.MVC.config
        /// </value>
        public static string IndexName
        {
            get
            {
                return Settings.GetSetting("WWW.IndexName", "sitecore_master_index");
            }
        }

    }
}
