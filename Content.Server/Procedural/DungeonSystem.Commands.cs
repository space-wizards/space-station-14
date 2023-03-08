using Content.Shared.Procedural;
using Content.Shared.Procedural.DungeonGenerators;
using Robust.Shared.Console;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server.Procedural;

public sealed partial class DungeonSystem
{
    private void VisualizeDungeon(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length < 3)
        {
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);
        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (!TryComp<MapGridComponent>(mapUid, out var mapGrid))
        {
            return;
        }

        if (!_prototype.TryIndex<DungeonConfigPrototype>(args[1], out var dungeon))
        {
            return;
        }

        if (!int.TryParse(args[2], out var seed))
        {
            return;
        }

        if (dungeon.Generator is not PrefabDunGen prefab)
        {
            return;
        }

        var rand = new Random(seed);
        var preset = prefab.Presets[rand.Next(prefab.Presets.Count)];
        var config = _prototype.Index<DungeonPresetPrototype>(preset);
        var rotation = GetDungeonRotation(seed);
        var dungeonMatrix = Matrix3.CreateRotation(rotation);

        foreach (var pack in config.RoomPacks)
        {
            // TODO: Finish this previs
            // var box =
        }
    }

    /// <summary>
    /// Generates a dungeon via command.
    /// </summary>
    private void GenerateDungeon(IConsoleShell shell, string argstr, string[] args)
    {
        if (args.Length < 2)
        {
            return;
        }

        if (!int.TryParse(args[0], out var mapInt))
        {
            return;
        }

        var mapId = new MapId(mapInt);
        var mapUid = _mapManager.GetMapEntityId(mapId);

        if (!TryComp<MapGridComponent>(mapUid, out var mapGrid))
        {
            return;
        }

        if (!_prototype.TryIndex<DungeonConfigPrototype>(args[1], out var dungeon))
        {
            return;
        }

        int seed;

        if (args.Length >= 3)
        {
            if (!int.TryParse(args[2], out seed))
            {
                return;
            }
        }
        else
        {
            seed = new Random().Next();
        }

        seed = 92959802;
        GenerateDungeon(dungeon, mapUid, mapGrid, seed);
    }
}
