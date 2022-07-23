using Content.Server.AI.Components;
using Content.Server.AI.HTN;

namespace Content.Server.AI.Systems;

public sealed partial class NPCSystem
{
    // Hierarchical Task Network
    private void InitializeHTN()
    {

    }

    private void UpdateHTN(float frameTime)
    {
        foreach (var (_, comp) in EntityQuery<ActiveNPCComponent, HTNComponent>())
        {
            if (_count >= _maxUpdates) break;

            Update(comp, frameTime);
            _count++;
        }
    }

    private void Update(HTNComponent component, float frameTime)
    {
        // TODO: Need a plan class to track things. Needs to track:
        // Full plan
        // Current step in plan

        // Get a new plan
        if (component.Plan == null)
        {
            return;
        }

        // Run the existing plan still
        var currentTask = component.Plan.CurrentTask;

        // Run the existing operator
    }
}
