using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Robust.Shared.Utility;

namespace Content.Server.Salvage;

public sealed partial class SalvageSystem
{
    private void InitializeRunner()
    {
    }

    // Runs the expedition
    private void UpdateRunner()
    {
        // Structure missions
        foreach (var (structure, comp) in EntityQuery<SalvageStructureExpeditionComponent, SalvageExpeditionComponent>())
        {
            if (comp.Completed)
                continue;

            for (var i = 0; i < structure.Structures.Count; i++)
            {
                var objective = structure.Structures[i];

                if (Deleted(objective))
                {
                    structure.Structures.RemoveSwap(i);
                }
            }

            if (structure.Structures.Count == 0)
            {
                var mission = comp.MissionParams;
                comp.Completed = true;

                // TODO: Announce completion
            }
        }
    }
}
