using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Procedural;
using Content.Shared.Salvage;
using Content.Shared.Salvage.Expeditions.Structure;
using Robust.Shared.Map.Components;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void SetupMission(ISalvageMission mission, Dungeon dungeon, MapGridComponent grid, Random random)
    {
        // TODO: Move this to the main method
        // TODO: Pass in the pre-built rooms and shit and then spawn via that.
        // TODO: Dungeon likely needs a start room or smth.
        switch (mission)
        {
            case SalvageStructure structure:
                SetupMission(structure, dungeon, grid, random);
                break;
            default:
                throw new NotImplementedException();
        }
    }

    private void SetupMission(SalvageStructure structure, Dungeon dungeon, MapGridComponent grid, Random random)
    {
        // TODO: Landing around dungeon + pathfind into it.
    }
}
