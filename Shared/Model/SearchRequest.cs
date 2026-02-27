using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Model
{
    public class SearchRequest
    {
        /// <summary>
        /// Search terms entered by the user.
        /// Example: ["halal", "pizza"]
        /// </summary>
        public string[] Query { get; set; } = Array.Empty<string>();

        /// <summary>
        /// Maximum number of results to return.
        /// Defaults to 10.
        /// </summary>
        public int MaxAmount { get; set; } = 10;

        /// <summary>
        /// If true, search is case-sensitive. Defaults to true to keep current behavior.
        /// Set to false to perform case-insensitive searches.
        /// </summary>
        public bool CaseSensitive { get; set; } = true;

    }

}
