using Robust.Server.GameObjects;
using Robust.Server.Maps;
using Robust.Shared.Map;
using Content.Server.GameTicking;
using Robust.Shared.Prototypes;
using Content.Server.GameTicking.Rules;

namespace Content.Server.StationEvents.Events;

public sealed class LoneOpsSpawn : StationEventSystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly MapLoaderSystem _map = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly NukeopsRuleSystem _nukeopsRuleSystem = default!;

    public override string Prototype => "LoneOpsSpawn";
    public const string LoneOpsShuttlePath = "Maps/Shuttles/striker.yml";
    public const string GameRuleProto = "Nukeops";

    public override void Started()
    {
        base.Started();

        if (!_nukeopsRuleSystem.CheckLoneOpsSpawn())
            return;

        var shuttleMap = _mapManager.CreateMap();
        var options = new MapLoadOptions()
        {
            LoadMap = true,
        };

        _map.TryLoad(shuttleMap, LoneOpsShuttlePath, out var grids, options);

        if (!_prototypeManager.TryIndex<GameRulePrototype>(GameRuleProto, out var ruleProto))
            return;

        _nukeopsRuleSystem.LoadLoneOpsConfig();
        _gameTicker.StartGameRule(ruleProto);
    }
}

