using System.Linq;
using Content.Server.Administration;
using Content.Server.Atmos;
using Content.Server.Atmos.Components;
using Content.Shared.Administration;
using Content.Shared.Atmos;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Parallax.Biomes;
using Robust.Shared.Audio;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Maps;

/// <summary>
/// Converts the supplied map into a "planet" with defaults.
/// </summary>
[AdminCommand(AdminFlags.Mapping)]
public sealed class PlanetCommand : IConsoleCommand
{
    [Dependency] private readonly IEntityManager _entManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPrototypeManager _protoManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public string Command => $"planet";
    public string Description => Loc.GetString("cmd-planet-desc");
    public string Help => Loc.GetString("cmd-planet-help", ("command", Command));
    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length != 2)
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

        if (!_protoManager.HasIndex<BiomePrototype>(args[1]))
        {
            shell.WriteError(Loc.GetString("cmd-planet-map-prototype", ("prototype", args[1])));
            return;
        }

        var mapUid = _mapManager.GetMapEntityId(mapId);
        MetaDataComponent? metadata = null;

        var biome = _entManager.EnsureComponent<BiomeComponent>(mapUid);
        biome.BiomePrototype = args[1];
        biome.Seed = _random.Next();
        _entManager.Dirty(biome);

        var gravity = _entManager.EnsureComponent<GravityComponent>(mapUid);
        gravity.Enabled = true;
        _entManager.Dirty(gravity, metadata);

        // Day lighting
        // Daylight: #D8B059
        // Midday: #E6CB8B
        // Moonlight: #2b3143
        // Lava: #A34931

        var light = _entManager.EnsureComponent<MapLightComponent>(mapUid);
        light.AmbientLightColor = Color.FromHex("#D8B059");
        _entManager.Dirty(light, metadata);

        // Atmos
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

        _entManager.EnsureComponent<MapGridComponent>(mapUid);
        shell.WriteLine(Loc.GetString("cmd-planet-success", ("mapId", mapId)));
    }

    public CompletionResult GetCompletion(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var options = _entManager.EntityQuery<MapComponent>(true)
                .Select(o => new CompletionOption(o.WorldMap.ToString(), "MapId"));

            return CompletionResult.FromOptions(options);
        }

        if (args.Length == 2)
        {
            var options = _protoManager.EnumeratePrototypes<BiomePrototype>()
                .Select(o => new CompletionOption(o.ID, "Biome"));
            return CompletionResult.FromOptions(options);
        }

        return CompletionResult.Empty;
    }
}
