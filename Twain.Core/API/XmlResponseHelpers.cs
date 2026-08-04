using System.Xml;

namespace Twain.Core.API;

internal static class XmlResponseHelpers
{
    /// <summary>
    /// Returns the value of a required XML attribute.
    /// Throws a descriptive exception when a malformed API response
    /// does not include the expected attribute.
    /// </summary>
    internal static string RequireAttributeValue(
        XmlNode node,
        string attributeName)
    {
        if (node == null)
            throw new ArgumentNullException("node");

        if (string.IsNullOrEmpty(attributeName))
            throw new ArgumentException(
                "Attribute name is required.",
                "attributeName");

        XmlAttribute attribute =
            node.Attributes == null
                ? null
                : node.Attributes[attributeName];

        if (attribute == null)
        {
            throw new InvalidOperationException(
                "Expected XML attribute '" +
                attributeName +
                "' was not found on <" +
                node.Name +
                ">.");
        }

        return attribute.Value;
    }
}