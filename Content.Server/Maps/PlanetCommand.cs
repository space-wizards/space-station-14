using System.Linq;
using Content.Server.Administration;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Parallax;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Maps;

/// <summary>
/// Converts the supplied map into a "planet" with defaults.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class PlanetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;

    public string Command => $"planet";
    public string Description => Loc.GetString("cmd-planet-desc");
    public string Help => Loc.GetString("cmd-planet-help", ("command", Command));
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 1)
        {
            shell.WriteError(Loc.GetString($"cmd-planet-args"));
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            shell.WriteError(Loc.GetString($"cmd-planet-map", ("map", mapInt)));
            return;
        }

        var mapId = new MapId(mapInt);

        if (!_mapManager.MapExists(mapId))
        {
            shell.WriteError(Loc.GetString($"cmd-planet-map", ("map", mapId)));
            return;
        }

        var mapUid = _mapManager.GetMapEntityId(mapId);
        MetaDataComponent? metadata = null;

        var parallax = _entManager.EnsureComponent<ParallaxComponent>(mapUid);
        parallax.Parallax = "Grass";
        _entManager.Dirty(parallax, metadata);
        var gravity = _entManager.EnsureComponent<GravityComponent>(mapUid);
        gravity.Enabled = true;
        _entManager.Dirty(gravity);
        _entManager.EnsureComponent<MapLightComponent>(mapUid);
        var atmos = _entManager.EnsureComponent<MapAtmosphereComponent>(mapUid);

        atmos.Space = false;
        var moles = new float[Atmospherics.AdjustedNumberOfGases];
        moles[(int) Gas.Oxygen] = 21.824779f;
        moles[(int) Gas.Nitrogen] = 82.10312f;

        atmos.Mixture = new GasMixture(2500)
        {
            Temperature = 293.15f,
            Moles = moles,
        };

        var footstep = _entManager.EnsureComponent<FootstepModifierComponent>(mapUid);
        footstep.Sound = new SoundCollectionSpecifier("FootstepGrass");
        _entManager.Dirty(footstep);

        _entManager.EnsureComponent<MapGridComponent>(mapUid);
        shell.WriteLine(Loc.GetString("cmd-planet-success", ("mapId", mapId)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length != 1)
        {
            return CompletionResult.Empty;
        }

        var options = _entManager.EntityQuery<MapComponent>(true)
            .Select(o => new CompletionOption(o.WorldMap.ToString(), "MapId"));

        return CompletionResult.FromOptions(options);
    }
}
