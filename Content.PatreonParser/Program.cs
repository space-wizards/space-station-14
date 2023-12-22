using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Content.PatreonParser;
using CsvHelper;
using CsvHelper.Configuration;
using static System.Environment;

var repository = new DirectoryInfo(Directory.GetCurrentDirectory()).Parent!.Parent!.Parent!.Parent!;
var patronsPath = Path.Combine(repository.FullName, "Resources/Credits/Patrons.yml");
if (!File.Exists(patronsPath))
{
    Console.WriteLine($"File {patronsPath} not found.");
    return;
}

Console.WriteLine($"Updating {patronsPath}");
Console.WriteLine("Is this correct? [Y/N]");
var response = Console.ReadLine()?.ToUpper();
if (response != "Y")
{
    Console.WriteLine("Exiting");
    return;
}

var delimiter = ",";
var hasHeaderRecord = false;
var mode = CsvMode.RFC4180;
var escape = '\'';
Console.WriteLine($"""
Delimiter: {delimiter}
HasHeaderRecord: {hasHeaderRecord}
Mode: {mode}
Escape Character: {escape}
""");

Console.WriteLine("Enter the full path to the .csv file containing the Patreon webhook data:");
var filePath = Console.ReadLine();
if (filePath == null)
{
    Console.Write("No path given.");
    return;
}

var file = File.OpenRead(filePath);
var csvConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
{
    Delimiter = delimiter,
    HasHeaderRecord = hasHeaderRecord,
    Mode = mode,
    Escape = escape,
};

using var reader = new CsvReader(new StreamReader(file), csvConfig);

// This does not handle tier name changes, but we haven't had any yet
var patrons = new Dictionary<Guid, Patron>();
var jsonOptions = new JsonSerializerOptions
{
    IncludeFields = true,
    NumberHandling = JsonNumberHandling.AllowReadingFromString
};

// This assumes that the rows are already sorted by id
foreach (var record in reader.GetRecords<Row>())
{
    if (record.Trigger == "members:create")
        continue;

    var content = JsonSerializer.Deserialize<Root>(record.ContentJson, jsonOptions)!;

    var id = Guid.Parse(content.Data.Id);
    patrons.Remove(id);

    var tiers = content.Data.Relationships.CurrentlyEntitledTiers.Data;
    if (tiers.Count == 0)
        continue;
    else if (tiers.Count > 1)
        throw new ArgumentException("Found more than one tier");

    var tier = tiers[0];
    var tierName = content.Included.SingleOrDefault(i => i.Id == tier.Id && i.Type == tier.Type)?.Attributes.Title;
    if (tierName == null || tierName == "Free")
        continue;

    if (record.Trigger == "members:delete")
        continue;

    var fullName = content.Data.Attributes.FullName.Trim();
    var pledgeStart = content.Data.Attributes.PledgeRelationshipStart;

    switch (record.Trigger)
    {
        case "members:create":
            break;
        case "members:delete":
            break;
        case "members:update":
            patrons.Add(id, new Patron(fullName, tierName, pledgeStart!.Value));
            break;
        case "members:pledge:create":
            if (pledgeStart == null)
                continue;

            patrons.Add(id, new Patron(fullName, tierName, pledgeStart.Value));
            break;
        case "members:pledge:delete":
            // Deleted pledge but still not expired, expired is handled earlier
            patrons.Add(id, new Patron(fullName, tierName, pledgeStart!.Value));
            break;
        case "members:pledge:update":
            patrons.Add(id, new Patron(fullName, tierName, pledgeStart!.Value));
            break;
    }
}

var patronList = patrons.Values.ToList();
patronList.Sort((a, b) => a.Start.CompareTo(b.Start));
var yaml = patronList.Select(p => $"""
- Name: "{p.FullName.Replace("\"", "\\\"")}"
  Tier: {p.TierName}
""");
var output = string.Join(NewLine, yaml) + NewLine;
File.WriteAllText(patronsPath, output);
Console.WriteLine($"Updated {patronsPath} with {patronList.Count} patrons.");
