using Robust.Shared.Player;

namespace Content.Shared.Tabletop;

public abstract partial class SharedTabletopSystem
{
    /// <summary>
    ///     Adds a player to a tabletop game session, sending a message so the tabletop window opens on their end.
    /// </summary>
    /// <param name="player">The player session in question.</param>
    /// <param name="uid">The UID of the tabletop game entity.</param>
    public virtual void OpenSessionFor(ICommonSession player, EntityUid uid) { }
}
