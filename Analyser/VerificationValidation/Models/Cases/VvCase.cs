using Analyser.VerificationValidation.Models.Cases.Logs;
using Analyser.VerificationValidation.Models.Cases.Procedures;
using System.Collections.Generic;

namespace Analyser.VerificationValidation.Models.Cases
{
    class VvCase : AbstractTextElement
    {
        public IEnumerable<VvProcedure> VvProcedures { get; set; }
        public IEnumerable<VvTargetRef> VvTargetRefs { get; set; }
        public IEnumerable<VvLog> VvLogs { get; set; }
        public string VvSubjectIrefs { get; set; }
    }
}
