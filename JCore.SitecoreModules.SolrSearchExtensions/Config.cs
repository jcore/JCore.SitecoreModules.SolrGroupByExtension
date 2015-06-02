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

        public static int SolrHighlightsNumberOfSnippets
        {
            get
            {
                return Settings.GetIntSetting("SolrHighlightsNumberOfSnippets", 5);
            }
        }

        public static string[] SolrHighlightsFields
        {
            get
            {
                var fields = Settings.GetSetting("SolrHighlightsFields","_content");
                return fields.Split(',');
            }
        }

        public static string SolrHighlightsRegexPattern
        {
            get
            {
                return Settings.GetSetting("SolrHighlightsRegexPattern", @"\w[^|;.!?]{50,400}[|;.!?]");
            }
        }

        public static int SolrHighlightsFragsize
        {
            get
            {
                return Settings.GetIntSetting("SolrHighlightsFragsize", 300);
            }
        }

        public static double SolrHighlightsRegexSlop
        {
            get
            {
                return Settings.GetDoubleSetting("SolrHighlightsRegexSlop", 0.2);
            }
        }

        public static string DateField
        {
            get
            {
                return Settings.GetSetting("SolrDateField", "date_tdt");
            }
        }
    }
}
