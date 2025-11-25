using Content.Shared.Interaction;
using Content.Shared.Interaction.Requirements;
using Content.Shared.Power.Components;

namespace Content.Shared.Power.EntitySystems;

public abstract class SharedInteractionRequirementPowerSystem : InteractionRequirementSystem<InteractionRequirementPowerComponent>
{
    protected override string FailureSuffix => "power";
}
