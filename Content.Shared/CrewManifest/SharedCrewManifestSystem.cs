using Robust.Shared.Serialization;

namespace Content.Shared.CrewManifest;

[Serializable, NetSerializable]
public sealed class CrewManifestEntries
{
    /// <summary>
    ///     Entries in the crew manifest. Goes by department ID.
    /// </summary>
    public Dictionary<string, List<CrewManifestEntry>> Entries = new();
}

[Serializable, NetSerializable]
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
