using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Trigger.Systems;

/// <summary>
/// Trigger system for melee related triggers.
/// </summary>
public sealed class TriggerOnMeleeTriggerSystem : TriggerOnXSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnMeleeMissComponent, MeleeHitEvent>(OnMissTrigger);
        SubscribeLocalEvent<TriggerOnMeleeSwingComponent, MeleeHitEvent>(OnSwingTrigger);
        SubscribeLocalEvent<TriggerOnMeleeHitComponent, MeleeHitEvent>(OnHitTrigger);
    }

    private void OnMissTrigger(Entity<TriggerOnMeleeMissComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            Trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }

    private void OnSwingTrigger(Entity<TriggerOnMeleeSwingComponent> ent, ref MeleeHitEvent args)
    {
        EntityUid? target;
        if  (args.HitEntities.Count == 0)
            target = ent.Comp.TargetIsUser ? null : args.User;
        else
            target = ent.Comp.TargetIsUser ? args.HitEntities[0] : args.User;

        Trigger.Trigger(ent.Owner, target, ent.Comp.KeyOut);
    }

    private void OnHitTrigger(Entity<TriggerOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (!ent.Comp.TriggerEveryHit)
        {
            var target = ent.Comp.TargetIsUser ? args.HitEntities[0] : args.User;
            Trigger.Trigger(ent.Owner, target, ent.Comp.KeyOut);
            return;
        }

        // if TriggerEveryHit
        foreach (var target in args.HitEntities)
        {
            Trigger.Trigger(ent.Owner, ent.Comp.TargetIsUser ? target : args.User, ent.Comp.KeyOut);
        }
    }
}
