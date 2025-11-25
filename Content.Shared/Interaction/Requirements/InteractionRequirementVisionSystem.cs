using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;

namespace Content.Shared.Interaction.Requirements;

[RegisterComponent]
public sealed partial class TrackedInteractionRequirementVisionComponent : TrackedInteractionRequirementComponent<InteractionRequirementVisionComponent>;

public sealed class InteractionRequirementVisionSystem : InteractionRequirementSystem<InteractionRequirementVisionComponent, TrackedInteractionRequirementVisionComponent>
{
    protected override string FailureSuffix => "vision";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BlindableComponent, BlindnessChangedEvent>(OnBlindnessChanged);
    }

    protected override bool Condition(Entity<InteractionRequirementVisionComponent> ent, ref readonly ConditionalInteractionAttemptEvent args)
    {
        if (!TryComp<BlindableComponent>(args.Source, out var blindable) || !blindable.IsBlind)
            return true;

        return false;
    }

    private void OnBlindnessChanged(Entity<BlindableComponent> ent, ref BlindnessChangedEvent args)
    {
        NotifyRequirementChangeSource(ent.Owner, !args.Blind);
    }
}
