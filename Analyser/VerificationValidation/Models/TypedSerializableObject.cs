using System.Xml.Linq;

namespace Analyser.VerificationValidation.Models
{
    public class TypedSerializableObject : AbstractSerializableObject
    {
        public string Type { get; set; }
        public string Value { get; set; }

        public override XElement Serialize(XNamespace ns)
        {
            var type = this.GetType();
            var element = new XElement(ns + GetLocalName(type.Name), new XAttribute(GetLocalName(nameof(Type)), Type))
            {
                Value = Value
            };
            return element;
        }
    }
}
