using System.Collections.Generic;

namespace Analyser
{
    public abstract class Constraint
    {
        public Constraint()
        {
            ProccessedStimulusTimestamps = new List<string>();
        }

        public string ShortName { get; set; }

        public double Value { get; set; }

        /// <summary>
        /// timestamps of all the stimuluses that have been proccessed. TODO maybe clean this up when queue is cleaned?
        /// </summary>
        public List<string> ProccessedStimulusTimestamps { get; private set; }

    }
}
