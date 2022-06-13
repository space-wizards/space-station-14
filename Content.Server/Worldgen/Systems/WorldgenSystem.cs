using Robust.Shared.Map;

namespace Content.Server.Worldgen.Systems;

/// <summary>
/// This handles global control of world generation, like configuration and clearing areas.
/// </summary>
public sealed class WorldgenSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }
}

public sealed class MapConfigurationEvent : EntityEventArgs
{
    public readonly MapId MapId;
    public readonly List<object> Configs;

    public MapConfigurationEvent(MapId mapId, List<object> configs)
    {
        MapId = mapId;
        Configs = configs;
    }
}
