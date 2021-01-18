using System.Linq;
using System.Xml.Linq;

namespace Analyser
{
    public static class XElementExtensions
    {
        public static string GetChildValueByLocalName(this XElement element, string localName)
        {
            return element.Elements()
                .Single(x => x.Name.LocalName == localName).Value;
        }
    }
}
