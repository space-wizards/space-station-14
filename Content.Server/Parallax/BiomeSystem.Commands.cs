using System.Linq;
using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Layers;
using Content.Shared.Parallax.Biomes.Markers;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    private void InitializeCommands()
    {
        _console.RegisterCommand("biome_clear", Loc.GetString("cmd-biome_clear-desc"), Loc.GetString("cmd-biome_clear-help"), BiomeClearCallback, BiomeClearCallbackHelper);
        _console.RegisterCommand("biome_addlayer", Loc.GetString("cmd-biome_addlayer-desc"), Loc.GetString("cmd-biome_addlayer-help"), AddLayerCallback, AddLayerCallbackHelp);
        _console.RegisterCommand("biome_addmarkerlayer", Loc.GetString("cmd-biome_addmarkerlayer-desc"), Loc.GetString("cmd-biome_addmarkerlayer-desc"), AddMarkerLayerCallback, AddMarkerLayerCallbackHelper);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void BiomeClearCallback(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 1)
        {
            return;
        }

        int.TryParse(args[0], out var mapInt);
        var mapId = new MapId(mapInt);
        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (_mapManager.MapExists(mapId) ||
            !TryComp<BiomeComponent>(mapUid, out var biome))
        {
            return;
        }

        ClearTemplate(mapUid, biome);
    }

    private CompletionResult BiomeClearCallbackHelper(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.Components<BiomeComponent>(args[0], EntityManager), "Biome");
        }

        return CompletionResult.Empty;
    }

    [AdminCommand(AdminFlags.Fun)]
    private void AddLayerCallback(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length < 3 || args.Length > 4)
        {
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);
        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (!_mapManager.MapExists(mapId) || !TryComp<BiomeComponent>(mapUid, out var biome))
        {
            return;
        }

        if (!ProtoManager.TryIndex<BiomeTemplatePrototype>(args[1], out var template))
        {
            return;
        }

        var offset = 0;

        if (args.Length == 4)
        {
            int.TryParse(args[3], out offset);
        }

        AddTemplate(mapUid, biome, args[2], template, offset);
    }

    private CompletionResult AddLayerCallbackHelp(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            return CompletionResult.FromHintOptions(CompletionHelper.MapIds(EntityManager), "Map ID");
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<BiomeTemplatePrototype>(proto: ProtoManager), "Biome template");
        }

        if (args.Length == 3)
        {
            if (int.TryParse(args[0], out var mapInt))
            {
                var mapId = new MapId(mapInt);

                if (TryComp<BiomeComponent>(_mapManager.GetMapEntityId(mapId), out var biome))
                {
                    var results = new List<string>();

                    foreach (var layer in biome.Layers)
                    {
                        if (layer is not BiomeDummyLayer dummy)
                            continue;

                        results.Add(dummy.ID);
                    }

                    return CompletionResult.FromHintOptions(results, "Dummy layer ID");
                }
            }
        }

        if (args.Length == 4)
        {
            return CompletionResult.FromHint("Seed offset");
        }

        return CompletionResult.Empty;
    }

    [AdminCommand(AdminFlags.Fun)]
    private void AddMarkerLayerCallback(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 2)
        {
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);

        if (!_mapManager.MapExists(mapId) || !TryComp<BiomeComponent>(_mapManager.GetMapEntityId(mapId), out var biome))
        {
            return;
        }

        if (!ProtoManager.HasIndex<BiomeMarkerLayerPrototype>(args[1]))
        {
            return;
        }

        if (!biome.MarkerLayers.Add(args[1]))
        {
            return;
        }

        biome.ForcedMarkerLayers.Add(args[1]);
    }

    private CompletionResult AddMarkerLayerCallbackHelper(IConsoleShell shell, string[] args)
    {
        if (args.Length == 1)
        {
            var allQuery = AllEntityQuery<MapComponent, BiomeComponent>();
            var options = new List<CompletionOption>();

            while (allQuery.MoveNext(out var mapComp, out _))
            {
                options.Add(new CompletionOption(mapComp.MapId.ToString()));
            }

            return CompletionResult.FromHintOptions(options, "Biome");
        }

        if (args.Length == 2)
        {
            return CompletionResult.FromHintOptions(
                CompletionHelper.PrototypeIDs<BiomeMarkerLayerPrototype>(proto: ProtoManager), "Marker");
        }

        return CompletionResult.Empty;
    }
}
