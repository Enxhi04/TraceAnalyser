using System.Collections.Generic;

namespace Analyser
{
    public class Log : Dictionary<string, string>
    {
        /// <summary>
        /// columns that changed compared to previous log
        /// </summary>
        public List<string> ChangedColumns { get; set; }
    }
}
