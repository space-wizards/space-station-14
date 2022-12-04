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
        foreach (var comp in EntityQuery<SalvageExpeditionComponent>())
        {
            if (comp.Phase == SalvagePhase.Initializing)
            {
                var faction = _prototypeManager.Index<SalvageFactionPrototype>(comp.Faction);
                var initialCost = 100f;
                var weights = faction.MobWeights.ToDictionary(o => o.Key, pair => pair.Value.Weight);

                foreach (var marker in comp.SpawnMarkers)
                {
                    var coordinates = Transform(marker).Coordinates;
                    var count = _random.Next(3, 8);
                    for (var i = 0; i < count; i++)
                    {
                        if (initialCost <= 0f)
                            break;

                        // TODO: Need to specify the actual weights on mobs.
                        var spawn = _random.Pick(weights);
                        initialCost -= faction.MobWeights[spawn].Cost;
                        Spawn(spawn, coordinates);
                    }

                    if (initialCost <= 0f)
                        break;
                }

                comp.Phase = SalvagePhase.Initialized;
            }

            // TODO: Build intensity
        }
    }
}
