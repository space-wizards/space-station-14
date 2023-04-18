using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.Parallax.Biomes;
using Content.Shared.Parallax.Biomes.Layers;
using Robust.Shared.Console;
using Robust.Shared.Map;

namespace Content.Server.Parallax;

public sealed partial class BiomeSystem
{
    private void InitializeCommands()
    {
        _console.RegisterCommand("biome_addlayer", "weh", "weh", AddLayerCallback, AddLayerCallbackHelp);
    }

    [AdminCommand(AdminFlags.Fun)]
    private void AddLayerCallback(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length != 3)
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

        if (!_proto.TryIndex<BiomeTemplatePrototype>(args[1], out var template))
        {
            return;
        }

        AddTemplate(biome, args[2], template);
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
                CompletionHelper.PrototypeIDs<BiomeTemplatePrototype>(proto: _proto), "Biome template");
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

        return CompletionResult.Empty;
    }
}
