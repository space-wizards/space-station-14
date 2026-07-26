namespace Content.Shared.Tabletop;

/// <summary>
/// A class that stores per-player data for tabletops.
/// </summary>
[Serializable]
public sealed class TabletopSessionPlayerData
{
    public EntityUid Camera { get; set; }
}
