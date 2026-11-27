using JetBrains.Annotations;

namespace Content.Shared.Station.Events;

/// <summary>
/// Directed event fired on a station when it is renamed.
/// </summary>
[PublicAPI]
public sealed class StationRenamedEvent(string oldName, string newName) : EntityEventArgs
{
    /// <summary>
    /// Prior name of the station.
    /// </summary>
    public string OldName = oldName;

    /// <summary>
    /// New name of the station.
    /// </summary>
    public string NewName = newName;
}
