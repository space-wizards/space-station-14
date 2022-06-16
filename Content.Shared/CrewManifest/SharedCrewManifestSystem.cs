namespace Content.Shared.CrewManifest;

public sealed class CrewManifestEntries
{
    Dictionary<string, List<CrewManifestEntry>> Entries = new();
}

public sealed class CrewManifestEntry
{
    public string Name { get; }

    public string JobTitle { get; }

    public int DisplayPriority { get; }

    public CrewManifestEntry(string name, string jobTitle, int displayPriority)
    {
        Name = name;
        JobTitle = jobTitle;
        DisplayPriority = displayPriority;
    }
}
