using Robust.Shared.Serialization;

namespace Content.Shared.CrewManifest;

/*

    So, you've found the crew manifest. There's a number of
    design decisions here made for many reasons, mostly:
    - How do you access the crew manifest UI from the lobby?
    - How would you access the crew manifest UI from entities
      that can view a crew manifest?

    It's messy, but BoundUserInterface could not answer these questions,
    mostly because I assumed that you can't convert one from one interface
    to another (parent->child relationship aside). You also probably
    wouldn't want to cache *all* station crew manifests.

    If you have any alternate solutions to this, feel free to actually
    try them out. Not even LateJoinGui has a BUI (it instead uses GameTickerSystem
    to get the current job listings).

*/
public abstract class SharedCrewManifestSystem : EntitySystem
{}

[Serializable, NetSerializable]
public sealed class RequestCrewManifestMessage : EntityEventArgs
{
    public EntityUid Station { get; }

    public RequestCrewManifestMessage(EntityUid station)
    {
        Station = station;
    }
}

[Serializable, NetSerializable]
public sealed class CrewManifestState : EntityEventArgs
{
    /// <summary>
    ///     The station this crew manifest state is for.
    ///     If this is null, this means that there is no
    ///     station related to this request, and it must
    ///     be handled appropriately.
    /// </summary>
    public EntityUid? Station { get; }

    /// <summary>
    ///     The entries for this crew manifest. See
    ///     <see cref="Station"/> for how to handle this
    ///     when this is null.
    /// </summary>
    public CrewManifestEntries? Entries { get; }

    public CrewManifestState(EntityUid? station, CrewManifestEntries? entries)
    {
        Station = station;
        Entries = entries;
    }
}

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

/// <summary>
///     Tells the server to open a crew manifest UI from
///     this entity's point of view.
/// </summary>
[Serializable, NetSerializable]
public sealed class CrewManifestOpenUiMessage : BoundUserInterfaceMessage
{}

[Serializable, NetSerializable]
public enum CrewManifestUiKey
{
    Key
}
