using Content.Shared.Popups;

namespace Content.Shared.Interaction.Requirements;

/// <summary>
/// <see cref="ActivatableUIRequiresAnchorComponent"/>
/// </summary>
public sealed class ActivatableUIRequiresAnchorSystem : InteractionRequirementSystem<InteractionRequirementAnchorComponent>
{
    protected override string FailureSuffix => "anchor";

    protected override bool Condition(Entity<InteractionRequirementAnchorComponent> ent, ref readonly ConditionalInteractionAttemptEvent args)
    {
        if (Transform(ent.Owner).Anchored)
            return true;

        return false;
    }
}
