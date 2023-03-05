using Content.Server._Citadel.Worldgen.Components;
using Content.Server._Citadel.Worldgen.Prototypes;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Robust.Shared.Configuration;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Server._Citadel.Worldgen.Systems;

/// <summary>
///     This handles configuring world generation during round start.
/// </summary>
public sealed class WorldgenConfigSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IMapManager _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _ser = default!;

    private bool _enabled;
    private string _worldgenConfig = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnLoadingMaps);
        _cfg.OnValueChanged(WorldgenCVars.WorldgenEnabled, b => _enabled = b, true);
        _cfg.OnValueChanged(WorldgenCVars.WorldgenConfig, s => _worldgenConfig = s, true);
    }

    /// <summary>
    ///     Applies the world config to the default map if enabled.
    /// </summary>
    private void OnLoadingMaps(RoundStartingEvent ev)
    {
        if (_enabled == false)
            return;

        var target = _map.GetMapEntityId(_gameTicker.DefaultMap);
        Logger.Debug($"Trying to configure {_gameTicker.DefaultMap}, aka {ToPrettyString(target)} aka {target}");
        var cfg = _proto.Index<WorldgenConfigPrototype>(_worldgenConfig);

        cfg.Apply(target, _ser, EntityManager); // Apply the config to the map.

        DebugTools.Assert(HasComp<WorldControllerComponent>(target));
    }
}

