using Content.Server.GameTicking;
using Content.Server.Worldgen.Prototypes;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server.Worldgen.Systems;

/// <summary>
/// This handles global control of world generation, like configuration and clearing areas.
/// </summary>
public sealed class WorldgenSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<PostGameMapLoad>(PostGameMapLoadEvent);
    }

    private void PostGameMapLoadEvent(PostGameMapLoad ev)
    {
        var config = ev.GameMap.WorldgenConfig;
        var configs = _prototypeManager.Index<WorldgenConfigPrototype>(config);
        RaiseLocalEvent(new MapConfigurationEvent(ev.Map, configs.ConfigData));
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
