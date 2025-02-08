using Robust.Shared.Serialization;

namespace Content.Shared.Tabletop.Events;

/// <summary>
/// An event ot tell the server that we have stopped playing this tabletop game.
/// </summary>
[Serializable, NetSerializable]
public sealed class TabletopStopPlayingEvent(NetEntity tableUid) : EntityEventArgs
{
    /// <summary>
    /// The entity UID of the table associated with this tabletop game.
    /// </summary>
    public NetEntity TableUid = tableUid;
}
