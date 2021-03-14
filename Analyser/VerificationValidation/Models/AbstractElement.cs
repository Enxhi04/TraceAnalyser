namespace Analyser.VerificationValidation.Models
{
    public class AbstractElement: AbstractSerializableObject
    {
        public string ShortName { get; set; }
        public string Name { get; set; }
        public string OwnedComments { get; set; }
        public string Category { get; set; }
    }
}
