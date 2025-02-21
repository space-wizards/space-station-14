using System.Numerics;
using Robust.Shared.Map;
using Robust.Shared.Player;

namespace Content.Server.Tabletop;

/// <summary>
///     A class for storing data about a running tabletop game.
/// </summary>
public sealed class TabletopSession(MapId tabletopMap, Vector2 position)
{
    /// <summary>
    ///     The center position of this session.
    /// </summary>
    public readonly MapCoordinates Position = new(position, tabletopMap);

    /// <summary>
    ///     The set of players currently playing this tabletop game.
    /// </summary>
    public readonly Dictionary<ICommonSession, PlayerData> Players = new();

    /// <summary>
    ///     All entities bound to this session. If you create an entity for this session, you have to add it here.
    /// </summary>
    public readonly HashSet<EntityUid> Entities = [];

    /// <summary>
    ///     A class that stores per-player data for tabletops.
    /// </summary>
    public sealed class PlayerData
    {
        public EntityUid Camera;
    }
}
