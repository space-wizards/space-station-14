using System.Linq;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Procedural;
using Content.Shared.Procedural.Dungeons;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions.Structure;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void SetupMission(ISalvageMission mission, Vector2i dungeonOffset, Dungeon dungeon, MapGridComponent grid, Random random)
    {
        // TODO: Move this to the main method
        // TODO: Pass in the pre-built rooms and shit and then spawn via that.
        // TODO: Dungeon likely needs a start room or smth.
        switch (mission)
        {
            case SalvageStructure structure:
                SetupMission(structure, dungeonOffset, dungeon, grid, random);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void SetupMission(SalvageStructure structure, Vector2i dungeonOffset, Dungeon dungeon, MapGridComponent grid, Random random)
    {
        // TODO: Uhh difficulty selection
        // TODO: Hardcoding
        var structureCount = random.Next(structure.MinStructures, structure.MaxStructures);
        var availableRooms = dungeon.Rooms.ToList();
        var faction = _prototypeManager.Index<SalvageFactionPrototype>("Xenos");
        var shaggy = (SalvageStructureFaction) faction.Configs["CaveStructures"];

        // Spawn the objectives
        for (var i = 0; i < structureCount; i++)
        {
            var structureRoom = _random.Pick(availableRooms);
            var spawnTile = _random.Pick(structureRoom.Tiles) + dungeonOffset;
            Spawn(shaggy.Spawn, grid.GridTileToLocal(spawnTile));
        }
    }
}
