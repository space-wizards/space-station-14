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
        // Mining missions: NOOP

        // Structure missions
        var structureQuery = EntityQueryEnumerator<SalvageStructureExpeditionComponent, SalvageExpeditionComponent>();

        while (structureQuery.MoveNext(out var uid, out var structure, out var comp))
        {
            if (comp.Completed)
                continue;

            for (var i = 0; i < structure.Structures.Count; i++)
            {
                var objective = structure.Structures[i];

                if (Deleted(objective))
                {
                    structure.Structures.RemoveSwap(i);
                    // TODO: Announce.
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
