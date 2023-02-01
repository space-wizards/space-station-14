using Content.Server.Procedural;
using Content.Server.Salvage.Expeditions;
using Robust.Shared.Map.Components;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void SetupMission(ISalvageMission mission, Dungeon dungeon, MapGridComponent grid, Random random)
    {
        // TODO: Move this to the main method
        // TODO: Pass in the pre-built rooms and shit and then spawn via that.
        // TODO: Dungeon likely needs a start room or smth.

    }
}
