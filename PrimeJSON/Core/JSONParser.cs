/* The MIT License (MIT)
 * 
 * Copyright (c) 2013 PrimeJSON
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to 
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 * 
 */

using System.Text.Json;

namespace PrimeJSON.Core;

public class JSONParser
{
    /// <summary>Parses one complete JSON value. Duplicate keys and depths above 64 are rejected.</summary>
    public static JSONNode Parse(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        try
        {
            using var document = JsonDocument.Parse(json, new JsonDocumentOptions
            {
                MaxDepth = 64,
                AllowDuplicateProperties = false
            });
            return ConvertElement(document.RootElement);
        }
        // Invalid Unicode escapes can surface during lazy string decoding.
        catch (Exception exception) when (exception is JsonException or InvalidOperationException)
        {
            throw new FormatException("Invalid JSON: " + exception.Message, exception);
        }
    }

    private static JSONNode ConvertElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var obj = new JSONObject();
                foreach (var property in element.EnumerateObject())
                    obj.Add(property.Name, ConvertElement(property.Value));
                return obj;
            case JsonValueKind.Array:
                var array = new JSONArray();
                foreach (var item in element.EnumerateArray())
                    array.Add(ConvertElement(item));
                return array;
            case JsonValueKind.String:
                return new JSONData(element.GetString());
            case JsonValueKind.Number:
                if (!element.TryGetDouble(out var number) || !double.IsFinite(number))
                    throw new FormatException("JSON number is outside the finite double range.");
                return new JSONData(number);
            case JsonValueKind.True:
                return new JSONData(true);
            case JsonValueKind.False:
                return new JSONData(false);
            case JsonValueKind.Null:
                return new JSONData((string?)null);
            default:
                throw new FormatException("Unsupported JSON value.");
        }
    }
}
