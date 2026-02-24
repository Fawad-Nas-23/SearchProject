using System;
using System.Collections.Generic;
using System.Text; 

namespace Shared.Model
{
    public class SearchResult
    {
        public String[] Query { get; set; }


        /// <summary>
        /// The total number of documents containing at least one word from the query
        /// </summary>
        public int NoOfHits { get; set; }

        /// <summary>
        /// The most important details about the documents hit by the query
        /// </summary>
        public List<DocumentHit> DocumentHits { get; set; }

        /// <summary>
        /// Words from the query that is ignored because they are not in any document
        /// </summary>
        public List<string> Ignored { get; set; }

        /// <summary>
        /// The care time used for the search
        /// </summary>
        public TimeSpan TimeUsed { get; set; }
    }

}
