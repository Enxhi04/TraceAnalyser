namespace Analyser.VerificationValidation
{
    public class VerificationValidationLog
    {
        public string Stimulus { get; set; }

        public string StimulusTimestamp { get; set; }

        public string Response { get; set; }

        public string ResponseTimestamp { get; set; }

        public string Constraint { get; set; }

        public string ConstraintType { get; set; }

        public double Value { get; set; }
    }
}
