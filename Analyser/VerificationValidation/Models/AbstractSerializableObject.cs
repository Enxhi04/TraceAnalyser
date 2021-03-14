using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace Analyser.VerificationValidation.Models
{
    public abstract class AbstractSerializableObject
    {
        public virtual XElement Serialize(XNamespace ns)
        {
            var type = GetType();
            var element = new XElement(ns + GetLocalName(type.Name));
            foreach (var property in type.GetProperties())
            {
                var value = property.GetValue(this);
                if (value is IEnumerable<object> list)// multiple elements
                {
                    var parentNode = new XElement(ns + GetLocalName(property.Name));
                    foreach (var listElement in list)
                    {
                        parentNode.Add(Serialize(listElement, ns, property.Name.Substring(0, property.Name.Length - 1)));
                    }
                    element.Add(parentNode);
                    continue;
                }
                var childElement = Serialize(value, ns, property.Name);
                element.Add(childElement);
            }
            return element;
        }

        private XElement Serialize(object value, XNamespace ns, string name)
        {
            if (value is AbstractSerializableObject serializableObject)
            {
                return serializableObject.Serialize(ns);
            }
            else
            {
                return new XElement(ns + GetLocalName(name))
                {
                    Value = value?.ToString() ?? ""
                };
            }
        }

        protected string GetLocalName(string propertyName)
        {
            if (string.IsNullOrWhiteSpace(propertyName))
            {
                throw new NullReferenceException("Property name cannot be null or whitespace");
            }
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
