using System.Collections.Generic;

namespace Analyser.VerificationValidation.Models.Cases.Procedures
{
    public class VvProcedure:  AbstractTextElement
    {
        public string VvStimulis { get; set; }

        public IEnumerable<VvIntendedOutcome> VvIntendedOutcomes { get; set; }
    }
}
