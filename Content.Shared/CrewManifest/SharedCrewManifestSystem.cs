using Robust.Shared.Serialization;

namespace Content.Shared.CrewManifest;

[Serializable, NetSerializable]
public sealed class CrewManifestBoundUiState: BoundUserInterfaceState
{
    public CrewManifestEntries? Entries { get; }

    public CrewManifestBoundUiState(CrewManifestEntries? entries)
    {
        Entries = entries;
    }
}

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

    public string JobIcon { get; }

    public int DisplayPriority { get; }

    public CrewManifestEntry(string name, string jobTitle, string jobIcon, int displayPriority)
    {
        Name = name;
        JobTitle = jobTitle;
        JobIcon = jobIcon;
        DisplayPriority = displayPriority;
    }
}

[Serializable, NetSerializable]
public enum CrewManifestUiKey
{
    Key
}
