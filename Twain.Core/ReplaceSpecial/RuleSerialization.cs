using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Twain.Core.ReplaceSpecial;

/// <summary>
/// Provides serialization support for Replace Special rules.
/// </summary>
public static class RuleSerialization
{
    /// <summary>
    /// Serializes a replacement rule to its XML representation.
    /// </summary>
    /// <param name="rule">
    /// The replacement rule to serialize.
    /// </param>
    /// <returns>
    /// An XML string containing the serialized replacement rule.
    /// </returns>
    public static string Serialize(
        IRule rule)
    {
        ArgumentNullException.ThrowIfNull(rule);

        var serializer =
            new XmlSerializer(typeof(IRule));

        using var stream =
            new MemoryStream();

        using (var writer =
            new XmlTextWriter(
                stream,
                Encoding.UTF8))
        {
            serializer.Serialize(
                writer,
                rule);
        }

        return Encoding.UTF8.GetString(
            stream.ToArray());
    }

    /// <summary>
    /// Deserializes a replacement rule from its XML representation.
    /// </summary>
    /// <param name="serializedRule">
    /// The XML representation of the replacement rule.
    /// </param>
    /// <returns>
    /// The deserialized replacement rule.
    /// </returns>
    public static IRule Deserialize(
        string serializedRule)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            serializedRule);

        if (!serializedRule.Contains(
                "<?xml",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "The supplied text is not an XML replacement rule.");
        }

        var serializer =
            new XmlSerializer(typeof(IRule));

        using var stream =
            new MemoryStream(
                Encoding.UTF8.GetBytes(
                    serializedRule));

        return serializer.Deserialize(stream) as IRule
            ?? throw new InvalidOperationException(
                "The supplied XML does not contain a valid replacement rule.");
    }
}