using System.Linq;
using Content.Server.Salvage.Expeditions;
using Content.Server.Salvage.Expeditions.Structure;
using Content.Shared.Random.Helpers;
using Content.Shared.Salvage;
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
        foreach (var (structure, comp) in EntityQuery<SalvageStructureExpeditionComponent, SalvageExpeditionDataComponent>())
        {
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
                comp.MissionCompleted = true;
            }
        }

        // Run missions
        // TODO: For dynamic ones
    }
}
