using System.Collections.Generic;

namespace Analyser.Constraints
{
    public class ReactionConstraint : Constraint
    {
        public ReactionConstraint()
        {
            StimulusResponses = new List<List<StimulusResponse>>();
        }

        public StimulusResponse InitialStimulusResponse { get; set; }

        public List<List<StimulusResponse>> StimulusResponses { get; set; }
    }
}
