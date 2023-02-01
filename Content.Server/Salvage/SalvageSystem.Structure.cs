using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Robust.Shared.Map.Components;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void SetupMission(ISalvageMission mission, IDungeonGenerator gen, DungeonConfig dunGenConfig, MapGridComponent grid, Random random)
    {
        // TODO: Move this to the main method
        // TODO: Pass in the pre-built rooms and shit and then spawn via that.
        // TODO: Dungeon likely needs a start room or smth.
        var dungeon = _dungeon.GetDungeon(gen);
        _dungeon.SpawnDungeon(dungeon, dunGenConfig, grid);
    }
}
