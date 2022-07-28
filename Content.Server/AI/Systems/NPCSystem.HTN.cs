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
        // Get a new plan
        if (component.Plan == null)
        {
            // TODO: Use an event and get it whenever.
            component.Plan = GetPlan(component);
            return;
        }

        // Run the existing plan still
        var currentOperator = component.Plan.CurrentOperator;

        // Run the existing operator
    }
}
