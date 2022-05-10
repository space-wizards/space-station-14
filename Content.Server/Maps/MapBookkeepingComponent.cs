using JetBrains.Annotations;

namespace Content.Server.Maps;

[RegisterComponent, Friend(typeof(GameMapSystem)), PublicAPI]
public sealed class MapBookkeepingComponent : Component
{
    /// <summary>
    /// The grids that make up the map.
    /// </summary>
    public HashSet<EntityUid> ComponentGrids = new();

    public string Prototype = string.Empty;

    public string MapName = string.Empty;
}
