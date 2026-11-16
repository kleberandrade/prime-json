using PrimeJSON.Core;

var project = JSONParser.Parse("""
    {"name":"PrimeJSON","versions":[1,2],"active":true}
    """);
project["name"].JSONString = "PrimeJSON modernized";
project["versions"].Add(new JSONData(3d));
project.Add("description", new JSONData("A small JSON tree.\nWith proper escaping."));
project.Add("optional", new JSONData((string?)null));
Console.WriteLine(project.ToJSON());

var reparsed = JSONParser.Parse(project.ToJSON());
if (reparsed["versions"].Count != 3 || reparsed["optional"].Type != "JSONNull")
    throw new InvalidOperationException("The round trip failed.");
Console.WriteLine($"Latest version: {reparsed["versions"][2].JSONNumber}");

try
{
    JSONParser.Parse("[1,]");
    throw new InvalidOperationException("Invalid JSON was accepted.");
}
catch (FormatException)
{
    Console.WriteLine("Malformed JSON is rejected.");
}
