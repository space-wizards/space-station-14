using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.StepTrigger.Systems;
using Robust.Shared.Physics.Components;

namespace Content.Shared.Mousetrap;

public sealed class MousetrapSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MousetrapComponent, BeforeDamageOnTriggerEvent>(BeforeDamageOnTrigger);
        SubscribeLocalEvent<MousetrapComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
    }

    // only allow step triggers to trigger if the trap is armed
    // TODO: refactor Steptriggers to get rid of this
    // they should just use the new trigger conditions
    private void OnStepTriggerAttempt(Entity<MousetrapComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (!TryComp<ItemToggleComponent>(ent, out var toggle))
            return;

        args.Continue |= toggle.Activated;
    }

    // scale the damage according to mass
    private void BeforeDamageOnTrigger(Entity<MousetrapComponent> ent, ref BeforeDamageOnTriggerEvent args)
    {
        if (TryComp(args.Tripper, out PhysicsComponent? physics) && physics.Mass != 0)
        {
            // The idea here is inverse,
            // Small - big damage,
            // Large - small damage
            // yes i punched numbers into a calculator until the graph looked right
            var scaledDamage = -50 * Math.Atan(physics.Mass - ent.Comp.MassBalance) + 25 * Math.PI;
            args.Damage *= scaledDamage;
        }
    }
}
