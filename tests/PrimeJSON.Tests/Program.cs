using System.Collections;
using System.Globalization;
using System.Text.Json;
using PrimeJSON.Core;

// Dependency-free regression runner: each failing case produces a nonzero exit code.
var tests = new List<(string Name, Action Body)>
{
    ("Empty containers and scalar roots", () =>
    {
        Equal(0, JSONParser.Parse("{}").Count);
        Equal(0, JSONParser.Parse("[]").Count);
        Equal("hello", JSONParser.Parse("\"hello\"").JSONString);
        Equal(true, JSONParser.Parse("true").JSONBoolean);
        Equal(false, JSONParser.Parse("false").JSONBoolean);
        Equal("JSONNull", JSONParser.Parse("null").Type);
        Equal(-12.5, JSONParser.Parse("-1.25e1").JSONNumber);
    }),
    ("Nested objects in arrays and JSON whitespace", () =>
    {
        var node = JSONParser.Parse(" \r\n\t[{\"x\":[]},{\"y\":{}},true,null,2] \t");
        Equal(5, node.Count);
        Equal(0, node[0]["x"].Count);
        Equal(0, node[1]["y"].Count);
        Equal(2d, node[4].JSONNumber);
    }),
    ("String escapes, Unicode, and escaped property names round trip", () =>
    {
        const string text = "quote\" slash\\ line\n tab\t return\r back\b form\f zero\0 café 😀";
        var node = new JSONObject();
        node.Add(text, new JSONData(text));
        var reparsed = JSONParser.Parse(node.ToJSON());
        Equal(text, reparsed[text].JSONString);
        Equal("A😀", JSONParser.Parse("\"\\u0041\\uD83D\\uDE00\"").JSONString);
        using var document = JsonDocument.Parse(node.ToJSON());
        Equal(text, document.RootElement.GetProperty(text).GetString());
    }),
    ("Numbers serialize independently of current culture", () =>
    {
        var original = CultureInfo.CurrentCulture;
        try
        {
            foreach (var culture in new[] { "pt-BR", "en-US", "fr-FR" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                Equal("1234.5", new JSONData(1234.5).ToJSON());
                Equal(0.001, JSONParser.Parse("1e-3").JSONNumber);
                Equal(double.MaxValue, JSONParser.Parse(new JSONData(double.MaxValue).ToJSON()).JSONNumber);
                Equal(double.Epsilon, JSONParser.Parse(new JSONData(double.Epsilon).ToJSON()).JSONNumber);
            }
        }
        finally { CultureInfo.CurrentCulture = original; }
    }),
    ("Scalar setters switch the serialized type", () =>
    {
        var data = new JSONData("old");
        data.JSONBoolean = true;
        Equal("true", data.ToJSON());
        data.JSONNumber = 42;
        Equal("42", data.ToJSON());
        data.JSONString = null;
        Equal("null", data.ToJSON());
        data.JSONString = "new";
        Equal("\"new\"", data.ToJSON());
    }),
    ("Array mutation, enumeration, and removals", () =>
    {
        var array = new JSONArray();
        var first = new JSONData(1d);
        var second = new JSONData(2d);
        array.Add(first);
        array.Add(second);
        Equal(2, ((IEnumerable)array).Cast<JSONNode>().Count());
        array[0] = second;
        Equal(second, array.Remove(0));
        Equal(second, array.Remove(second));
        Equal("[]", array.ToJSON());
        Throws<ArgumentOutOfRangeException>(() => array.Remove(0));
        Throws<ArgumentNullException>(() => array.Add(null!));
        array.Add(first);
        Throws<ArgumentNullException>(() => array[0] = null!);
    }),
    ("Object mutation, enumeration, and removals", () =>
    {
        var obj = new JSONObject();
        var data = new JSONData(true);
        obj.Add("a", data);
        obj["b"] = new JSONData(false);
        Equal(2, ((IEnumerable)obj).Cast<KeyValuePair<string, JSONNode>>().Count());
        obj[0] = data;
        Equal(data, obj.Remove("a"));
        Equal(false, obj.Remove(0).JSONBoolean);
        obj.Add("c", data);
        Equal(data, obj.Remove(data));
        Equal("{}", obj.ToJSON());
        Throws<KeyNotFoundException>(() => obj.Remove("missing"));
        Throws<ArgumentOutOfRangeException>(() => obj.Remove(data));
        Throws<ArgumentOutOfRangeException>(() => _ = obj[-1]);
        Throws<ArgumentNullException>(() => obj.Add("x", null!));
        Throws<ArgumentNullException>(() => obj["x"] = null!);
        obj.Add("x", data);
        Throws<ArgumentNullException>(() => obj[0] = null!);
        Throws<ArgumentException>(() => obj.Add("x", data));
    }),
    ("Nonfinite values are rejected without changing the scalar", () =>
    {
        foreach (var value in new[] { double.NaN, double.PositiveInfinity, double.NegativeInfinity })
        {
            Throws<ArgumentOutOfRangeException>(() => _ = new JSONData(value));
            var data = new JSONData(1d);
            Throws<ArgumentOutOfRangeException>(() => data.JSONNumber = value);
            Equal("1", data.ToJSON());
        }
    }),
    ("Null input has an explicit argument error", () => Throws<ArgumentNullException>(() => JSONParser.Parse(null!))),
    ("Cycles fail but shared children serialize", () =>
    {
        var array = new JSONArray();
        array.Add(array);
        Throws<InvalidOperationException>(() => array.ToJSON());
        var obj = new JSONObject();
        obj.Add("self", obj);
        Throws<InvalidOperationException>(() => obj.ToJSON());
        var shared = new JSONArray();
        var parent = new JSONArray();
        parent.Add(shared);
        parent.Add(shared);
        Equal("[[],[]]", parent.ToJSON());
        shared._value.Add(null!); // Legacy public storage must fail explicitly too.
        Throws<InvalidOperationException>(() => shared.ToJSON());
    }),
    ("Depth limit is enforced on parsing and serialization", () =>
    {
        var valid = new string('[', 64) + "0" + new string(']', 64);
        Equal(valid, JSONParser.Parse(valid).ToJSON());
        Throws<FormatException>(() => JSONParser.Parse("[" + valid + "]"));
        var root = new JSONArray();
        var current = root;
        for (var i = 0; i < 64; i++)
        {
            var child = new JSONArray();
            current.Add(child);
            current = child;
        }
        Throws<InvalidOperationException>(() => root.ToJSON());
    })
};

string[] invalidInputs =
[
    "", " ", "[", "{", "[1", "{\"a\":1", "[1,]", "{\"a\":1,}", "[1 2]",
    "{\"a\" 1}", "{a:1}", "{} garbage", "true false", "[tru]", "[False]", "[nul]",
    "[01]", "[+1]", "[1.]", "[.1]", "[1e]", "[1e+]", "[--1]", "[1.2.3]",
    "[NaN]", "[Infinity]", "1e400", "-1e400", "\"unterminated", "\"\\q\"",
    "\"\\uZZZZ\"", "\"line\nbreak\"", "{/*comment*/}", "[1] []",
    "{\"a\":1,\"a\":2}", "{\"a\":1,\"\\u0061\":2}", "\uFEFF{}"
];
invalidInputs = [.. invalidInputs, "\"\\uD800\"", "\"\\uDC00\"", "\"\\uD800x\"", "{\"\\uD800\":1}"];
foreach (var input in invalidInputs)
    tests.Add(($"Reject malformed input {JsonSerializer.Serialize(input)}", () => Throws<FormatException>(() => JSONParser.Parse(input))));

var failures = 0;
foreach (var (name, body) in tests)
{
    try { body(); Console.WriteLine($"PASS {name}"); }
    catch (Exception exception) { failures++; Console.Error.WriteLine($"FAIL {name}: {exception}"); }
}
Console.WriteLine($"{tests.Count - failures}/{tests.Count} cases passed.");
return failures == 0 ? 0 : 1;

static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual))
        throw new Exception($"Expected {expected}, got {actual}.");
}

static void Throws<T>(Action action) where T : Exception
{
    try { action(); }
    catch (T) { return; }
    throw new Exception($"Expected {typeof(T).Name}.");
}
