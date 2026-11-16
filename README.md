# PrimeJSON

[![C#](https://img.shields.io/badge/language-C%23-512BD4)](https://learn.microsoft.com/dotnet/csharp/)
[![.NET 10](https://img.shields.io/badge/.NET-10-512BD4)](https://dotnet.microsoft.com/download/dotnet/10.0)
[![System.Text.Json](https://img.shields.io/badge/JSON-System.Text.Json-512BD4)](https://learn.microsoft.com/dotnet/standard/serialization/system-text-json/overview)
[![CI](https://github.com/kleberandrade/prime-json/actions/workflows/ci.yml/badge.svg?branch=main)](https://github.com/kleberandrade/prime-json/actions/workflows/ci.yml?query=branch%3Amain)
[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

A small C# library for parsing, editing, and writing JSON as a mutable tree.
It provides `JSONObject`, `JSONArray`, and `JSONData` nodes rather than mapping
JSON to application classes. Originally written for .NET Framework 4.5, this
version targets **.NET 10** and uses the built-in `System.Text.Json` parser and
writer while retaining the `PrimeJSON.Core` node API.

## Requirements

- A stable [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0).
- Windows, macOS, or Linux supported by that SDK.
- No external NuGet packages or services are required.

`global.json` selects a stable .NET 10 SDK (10.0.100 or a later feature band).
The old .NET Framework 4.5 target is no longer supported. This is a source-level
migration; applications must rebuild for .NET 10.

## Installation

Clone and build from source:

```sh
git clone https://github.com/kleberandrade/prime-json.git
cd prime-json
dotnet build PrimeJSON.sln --configuration Release
```

Reference the library from your .NET 10 application:

```sh
dotnet add /path/to/YourApp.csproj reference /path/to/prime-json/PrimeJSON/PrimeJSON.csproj
```

These instructions use a project reference; they do not depend on a published
NuGet package. To produce a local package for inspection:

```sh
dotnet pack PrimeJSON/PrimeJSON.csproj --configuration Release --output artifacts
```

## Quick start

```csharp
using PrimeJSON.Core;

JSONNode document = JSONParser.Parse("""
    {"name":"PrimeJSON","versions":[1,2],"active":true}
    """);

document["name"].JSONString = "Updated project";
document["versions"].Add(new JSONData(3d));
document.Add("optional", new JSONData((string?)null));
Console.WriteLine(document.ToJSON());
```

`ToJSON()` writes compact, valid JSON, escaping both strings and property names.
Whitespace and escape spelling from the input are not preserved.

## Working with nodes

Import `PrimeJSON.Core`. `JSONParser.Parse(string)` accepts exactly one JSON
value, including scalar roots, with optional surrounding JSON whitespace.

- **Objects:** `Add(key, node)` adds a new member; `obj[key] = node` adds or
  replaces it. `obj[key]` reads a member. `Remove(key)`, `Remove(index)`, and
  `Remove(node)` return the removed value.
- **Arrays:** `Add(node)` appends; `array[index]` reads or replaces a value.
  `Remove(index)` returns the removed value. `Remove(node)` removes the first
  matching reference if present and returns the supplied node.
- **Scalars:** `new JSONData(string?)`, `new JSONData(bool)`, and
  `new JSONData(double)` construct string/null, Boolean, and number values.
  Use `3d` for a number to make the double overload explicit.
- **Types:** `Type` returns `JSONObject`, `JSONArray`, `JSONString`,
  `JSONBoolean`, `JSONNumber`, or `JSONNull` for built-in nodes. Inspect it
  before using `JSONString`, `JSONBoolean`, or `JSONNumber`.
- **Mutation:** setting a scalar property changes its active type. Inactive
  scalar getters retain their backing values and do not perform conversions.
- **Enumeration:** arrays yield `JSONNode` values; objects yield
  `KeyValuePair<string, JSONNode>` entries through the legacy non-generic
  `IEnumerable` interface. `Count` applies to containers.

Build a tree without parsing:

```csharp
var tags = new JSONArray();
tags.Add(new JSONData("C#"));
tags.Add(new JSONData("JSON"));

var item = new JSONObject();
item.Add("tags", tags);
item.Add("enabled", new JSONData(true));
item.Add("note", new JSONData((string?)null));

Console.WriteLine(item["tags"][0].JSONString); // C#
Console.WriteLine(item.ToJSON());
```

### Errors

```csharp
try
{
    JSONNode value = JSONParser.Parse("[1,]");
}
catch (FormatException error)
{
    Console.WriteLine(error.Message);
}
```

Malformed, incomplete, or trailing input, comments, trailing commas, duplicate
property names (including equivalent escaped names), invalid Unicode surrogate
escapes, and numeric overflow raise
`FormatException`. A C# null input raises `ArgumentNullException`.

NaN and infinities raise `ArgumentOutOfRangeException` in numeric constructors
and setters. Use `new JSONData((string?)null)` for JSON null. Container insertion
and replacement reject C# null node references with `ArgumentNullException`.
Missing object keys raise `KeyNotFoundException`; invalid positional indexes
raise `ArgumentOutOfRangeException`. Adding an existing key raises
`ArgumentException`. Operations that do not apply to a node, such as `Count`
on a scalar, retain the legacy `NotImplementedException` behavior.

## Executable example

```sh
dotnet run --project examples/PrimeJSON.Examples --configuration Release
```

The example parses and modifies a document, writes it, checks a round trip,
and demonstrates malformed-input handling. It exits unsuccessfully if its
checks fail.

## Build and tests

Run from the repository root:

```sh
dotnet build PrimeJSON.sln --configuration Release
dotnet run --project tests/PrimeJSON.Tests --configuration Release
dotnet run --project examples/PrimeJSON.Examples --configuration Release
```

The regression suite is a dependency-free console runner, **not a test-adapter
project**: use the `dotnet run` command above to execute it; `dotnet test` does
not run these cases. Each case prints PASS or FAIL and any failure produces a
nonzero exit code. Tests cover container mutation and enumeration, scalar roots,
escaping and Unicode, nested and empty containers, culture-independent numbers,
invalid syntax, duplicate keys, overflow, null handling, cycles, shared children,
and the depth limit. CI runs the build, suite, example, and local packaging on
Linux, macOS, and Windows. A configured workflow is not evidence that remote
CI has already passed.

Local verification on macOS ARM64 with SDK 10.0.404 completed successfully:
Release build with zero warnings/errors, all 52 regression cases, the executable
example, and `dotnet pack`. The generated package includes the library, README,
and license. Linux and Windows execution remains to be verified by CI.

## Limits and compatibility

- Numbers remain IEEE 754 `double`, as in the original API. Large integers
  (especially above 2^53) and decimal fractions can lose precision. Tiny values
  can underflow to zero. Original number spelling is not retained. Use
  `System.Text.Json` directly when exact decimal/integer handling is required.
- Parsing and writing allow at most 64 nested containers. Deeper input raises
  `FormatException`; deeper trees raise `InvalidOperationException` on writing.
  Cyclic trees also raise `InvalidOperationException`. Shared child references
  without cycles are allowed and serialized at each occurrence.
- The whole input and output are held in memory. There is no streaming API,
  schema validation, object mapping, or concurrency protection. Callers must
  bound input size and synchronize access when necessary.
- Legacy public `_value` collections remain available for compatibility. Prefer
  the node methods: direct collection mutation bypasses null checks. Writing a
  tree containing a C# null reference fails with `InvalidOperationException`.
- Object index access follows dictionary enumeration and is linear time. Prefer
  named keys; JSON object order is not an application-level contract.
- Serialization supports the three built-in node classes. A custom `JSONNode`
  subclass nested in a built-in container raises `NotSupportedException`.
- This update intentionally tightens invalid-input handling. Inputs accepted
  accidentally by the old parser can now fail. Output formatting is compact
  and may use Unicode escapes; compare parsed values rather than output bytes.

## License

[MIT](LICENSE), attributed to Kleber Andrade. The original 2013 PrimeJSON MIT
notices are preserved in the source and in the root license.
