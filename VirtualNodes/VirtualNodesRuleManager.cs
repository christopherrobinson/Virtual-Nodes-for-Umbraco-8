using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;

namespace VirtualNodes
{
    /// <summary>
    /// Loads rules for VirtualNodesUrlProvider
    /// </summary>
    public sealed class VirtualNodesRuleManager
    {
        #region Private Members

        /// <summary>
        /// Lazy singleton instance member
        /// </summary>
        private static readonly Lazy<VirtualNodesRuleManager> _instance = new Lazy<VirtualNodesRuleManager>(() => {
            var builder = new ConfigurationBuilder()
                        .SetBasePath(Directory.GetCurrentDirectory())
                        .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                        .AddEnvironmentVariables();

            IConfigurationRoot configuration = builder.Build();
            return new VirtualNodesRuleManager(configuration);
        });

        #endregion

        #region Public Members

        /// <summary>
        /// Gets the list of rules
        /// </summary>
        public List<string> Rules { get; }

        #endregion

        #region Constructors

        /// <summary>
        /// Returns a (singleton) VirtualNodesRuleManager instance
        /// </summary>
        public static VirtualNodesRuleManager Instance { get { return _instance.Value; } }


        /// <summary>
        /// Private constructor for Singleton
        /// </summary>
        private VirtualNodesRuleManager(IConfiguration configuration)
        {
            Rules = new List<string>();
            //Get all entries with keys starting with specified prefix
            // Add Node to appsettings.json 
            // "VirtualNodes" : {
            //   "Rules": "folderDoctype1, folderDoctype2, etc"
            // }
            var rules = configuration["VirtualNodes:Rules"];

            if (string.IsNullOrEmpty(rules))
            {
                return;
            }

            //Register a rule for each item
            foreach (string rule in rules.Split(','))
            {
                Rules.Add(rule.Trim());
            }
        }

        #endregion
    }
}
