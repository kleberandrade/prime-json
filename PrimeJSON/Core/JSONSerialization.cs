using System.Text;
using System.Text.Json;

namespace PrimeJSON.Core;

internal static class JSONSerialization
{
    internal static string Serialize(JSONNode node)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { MaxDepth = 64 }))
            Write(writer, node, new HashSet<JSONNode>(ReferenceEqualityComparer.Instance));
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void Write(Utf8JsonWriter writer, JSONNode node, HashSet<JSONNode> ancestors)
    {
        if (node is null)
            throw new InvalidOperationException("Use new JSONData((string?)null) for a JSON null value.");
        if (!ancestors.Add(node))
            throw new InvalidOperationException("A JSON tree cannot contain cycles.");
        try
        {
            switch (node)
            {
                case JSONArray array:
                    writer.WriteStartArray();
                    foreach (var child in array._value)
                        Write(writer, child, ancestors);
                    writer.WriteEndArray();
                    break;
                case JSONObject obj:
                    writer.WriteStartObject();
                    foreach (var pair in obj._value)
                    {
                        writer.WritePropertyName(pair.Key);
                        Write(writer, pair.Value, ancestors);
                    }
                    writer.WriteEndObject();
                    break;
                case JSONData data:
                    switch (data.Type)
                    {
                        case "JSONNull": writer.WriteNullValue(); break;
                        case "JSONString": writer.WriteStringValue(data.JSONString); break;
                        case "JSONBoolean": writer.WriteBooleanValue(data.JSONBoolean); break;
                        case "JSONNumber": writer.WriteNumberValue(data.JSONNumber); break;
                        default: throw new InvalidOperationException("Unknown scalar type.");
                    }
                    break;
                default:
                    throw new NotSupportedException("Only PrimeJSON's built-in node types can be serialized.");
            }
        }
        finally
        {
            ancestors.Remove(node);
        }
    }
}
