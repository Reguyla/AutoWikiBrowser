using System;
using System.Xml.Linq;

namespace WikiFunctions.API
{
    internal static class XmlResponseHelpers
    {
        internal static XDocument ParseApiXml(string xml)
        {
            if (string.IsNullOrWhiteSpace(xml))
                throw new ArgumentException("API response XML was empty.", nameof(xml));

            return XDocument.Parse(xml);
        }

        internal static XElement RequireRoot(XDocument doc, string expectedName = "api")
        {
            if (doc.Root == null)
                throw new InvalidOperationException("API response did not contain a root element.");

            if (!string.Equals(doc.Root.Name.LocalName, expectedName, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Unexpected API response root element: " + doc.Root.Name.LocalName);

            return doc.Root;
        }

        internal static XElement ElementOrNull(this XElement element, string name)
        {
            return element.Element(name);
        }

        internal static string AttributeValue(this XElement element, string name)
        {
            return element.Attribute(name)?.Value ?? string.Empty;
        }

        internal static bool HasAttribute(this XElement element, string name)
        {
            return element.Attribute(name) != null;
        }

        internal static XElement FirstDescendant(this XElement element, string name)
        {
            foreach (XElement descendant in element.Descendants(name))
                return descendant;

            return null;
        }
    }
}
