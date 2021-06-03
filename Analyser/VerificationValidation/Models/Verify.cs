namespace Analyser.VerificationValidation.Models
{
    public class Verify: SerializableObject
    {
        public string ShortName { get; set; }
        public string Name { get; set; }
        public string OwnedComments { get; set; }
        public string VerifiedByCaseRefs { get; set; }
        public string VerifiedByProcedureRefs { get; set; }
        public string VerifiedRequirementRefs { get; set; }
    }
}
