using System.Linq;
using System.Xml.Linq;

namespace Analyser
{
    public static class XElementExtensions
    {
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
