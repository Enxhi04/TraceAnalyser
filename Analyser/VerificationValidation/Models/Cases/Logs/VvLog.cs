using System.Collections.Generic;

namespace Analyser.VerificationValidation.Models.Cases.Logs
{
    public class VvLog: AbstractTextElement
    {
        public string Date { get; set; }

        public List<VvActualOutcome> VvActualOutcomes { get; set; }
    }
}
