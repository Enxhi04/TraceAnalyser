using System.Collections.Generic;

namespace Analyser.VerificationValidation.Models.Verifies
{
    public class Verify: AbstractElement
    {
        public List<VerifiedByCaseRef> VerifiedByCaseRefs { get; set; }
        public string VerifiedByProcedureRefs { get; set; }
        public string VerifiedRequirementRefs { get; set; }
    }
}
