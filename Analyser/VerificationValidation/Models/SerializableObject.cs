using System.Text;
using System.Xml.Linq;

namespace Analyser.VerificationValidation.Models
{
    public abstract class SerializableObject
    {
        public virtual XElement Serialize(XNamespace ns)
        {
            var type = GetType();
            var element = new XElement(ns+ GetLocalName(type.Name));
            foreach(var property in type.GetProperties())
            {
                if (typeof(SerializableObject).IsAssignableFrom(property.PropertyType))
                {
                    var childElement = ((SerializableObject)property.GetValue(this)).Serialize(ns);
                    element.Add(childElement);
                }
                else
                {
                    var childElement = new XElement(ns+ GetLocalName(property.Name))
                    {
                        Value = property.GetValue(this)?.ToString() ?? ""
                    };
                    element.Add(childElement);
                }
            }
            return element;
        }

        protected string GetLocalName(string propertyName)
        {
            var localName = new StringBuilder();
            foreach (char letter in propertyName)
            {
                if (char.IsUpper(letter))
                {
                    localName.Append("-" + letter);
                }
                else
                {
                    localName.Append(char.ToUpper(letter));
                }
            }
            if (localName[0] == '-')
            {
                localName.Remove(0, 1);
            }
            return localName.ToString();

        }
    }
}
