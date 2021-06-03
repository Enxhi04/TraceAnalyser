using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Analyser
{
    public static class XElementExtensions
    {
        public static List<string> GetChildrenValueByLocalName(this XElement element, string localName)
        {
            return element.Elements()
                .Where(x => x.Name.LocalName == localName)
                .Select(x=> x.Value)
                .ToList();
        }

        public static string GetChildValueByLocalName(this XElement element, string localName)
        {
            return element.GetChildByLocalName(localName)?.Value;
        }

        public static XElement GetChildByLocalName(this XElement element, string localName)
        {
            return element.Elements()
                .FirstOrDefault(x => x.Name.LocalName == localName);
        }
    }
}
