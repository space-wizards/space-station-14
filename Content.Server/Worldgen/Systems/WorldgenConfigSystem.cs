using Content.Server.Administration;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Worldgen.Components;
using Content.Server.Worldgen.Prototypes;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Utility;

namespace Content.Server.Worldgen.Systems;

/// <summary>
///     This handles configuring world generation during round start.
/// </summary>
public sealed class WorldgenConfigSystem : EntitySystem
{
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly ISerializationManager _ser = default!;

    private bool _enabled;
    private string _worldgenConfig = default!;

    /// <inheritdoc />
    public override void Initialize()
    {
        SubscribeLocalEvent<RoundStartingEvent>(OnLoadingMaps);
        _conHost.RegisterCommand("applyworldgenconfig", Loc.GetString("cmd-applyworldgenconfig-description"), Loc.GetString("cmd-applyworldgenconfig-help"), ApplyWorldgenConfigCommand);
        Subs.CVar(_cfg, CCVars.WorldgenEnabled, b => _enabled = b, true);
        Subs.CVar(_cfg, CCVars.WorldgenConfig, s => _worldgenConfig = s, true);
    }

    [AdminCommand(AdminFlags.Mapping)]
    private void ApplyWorldgenConfigCommand(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
        {
            shell.WriteError(Loc.GetString("shell-wrong-arguments-number-need-specific", ("properAmount", 2), ("currentAmount", args.Length)));
            return;
        }

        if (!int.TryParse(args[0], out var mapInt) || !_map.MapExists(new MapId(mapInt)))
        {
            shell.WriteError(Loc.GetString("shell-invalid-map-id"));
            return;
        }

        var map = _map.GetMapOrInvalid(new MapId(mapInt));

        if (!_proto.TryIndex<WorldgenConfigPrototype>(args[1], out var proto))
        {
            shell.WriteError(Loc.GetString("shell-argument-must-be-prototype", ("index", 2), ("prototypeName", "cmd-applyworldgenconfig-prototype")));
            return;
        }

        proto.Apply(map, _ser, EntityManager);
        shell.WriteLine(Loc.GetString("cmd-applyworldgenconfig-success"));
    }

    /// <summary>
    ///     Applies the world config to the default map if enabled.
    /// </summary>
    private void OnLoadingMaps(RoundStartingEvent ev)
    {
        if (_enabled == false)
            return;

        var target = _map.GetMapOrInvalid(_gameTicker.DefaultMap);
        Log.Debug($"Trying to configure {_gameTicker.DefaultMap}, aka {ToPrettyString(target)} aka {target}");
        var cfg = _proto.Index<WorldgenConfigPrototype>(_worldgenConfig);

        cfg.Apply(target, _ser, EntityManager); // Apply the config to the map.

        DebugTools.Assert(HasComp<WorldControllerComponent>(target));
    }
}

