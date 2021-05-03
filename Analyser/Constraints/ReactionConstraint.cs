using System.Collections.Generic;

namespace Analyser.Constraints
{
    public class ReactionConstraint : Constraint
    {
        public List<StimulusResponse> StimulusResponses { get; set; }
    }
}
