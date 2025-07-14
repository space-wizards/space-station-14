using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Physics.Events;

namespace Content.Shared.Trigger.Systems;

public sealed partial class TriggerSystem : EntitySystem
{
    private void InitializeCollide()
    {
        SubscribeLocalEvent<TriggerOnCollideComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<TriggerOnStepTriggerComponent, StepTriggeredOffEvent>(OnStepTriggered);
    }

    private void OnCollide(Entity<TriggerOnCollideComponent> ent, ref StartCollideEvent args)
    {
        if (args.OurFixtureId == ent.Comp.FixtureID && (!ent.Comp.IgnoreOtherNonHard || args.OtherFixture.Hard))
            Trigger(ent.Owner, args.OtherEntity, ent.Comp.TriggerKey);
    }

    private void OnStepTriggered(Entity<TriggerOnStepTriggerComponent> ent, ref StepTriggeredOffEvent args)
    {
        Trigger(ent, args.Tripper, ent.Comp.TriggerKey);
    }
}
