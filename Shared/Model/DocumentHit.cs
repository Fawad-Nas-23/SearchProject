using System;
using System.Collections.Generic;
using System.Text;

namespace Shared.Model
{
    public class DocumentHit
    {
        /// <summary>
        /// Represent a document in a search result. 
        /// </summary>

        public BEDocument Document { get; set; }

        /// <summary>
        /// The number of words from the query that is in the document
        /// </summary>
        public int NoOfHits { get; set; }

        /// <summary>
        /// The words from the query, that is not present in the document
        /// </summary>
        public List<string> Missing { get; set; }
    }
}
