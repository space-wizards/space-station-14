using System.Linq;
using Content.Shared.Trigger.Components.Triggers;
using Content.Shared.Weapons.Melee.Events;

namespace Content.Shared.Trigger.Systems;

public sealed class MeleeTriggerSystem : EntitySystem
{
    [Dependency] private readonly TriggerSystem _trigger = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TriggerOnMeleeHitComponent, MeleeHitEvent>(OnHitTrigger);
        SubscribeLocalEvent<TriggerOnMeleeMissComponent, MeleeHitEvent>(OnMissTrigger);
        SubscribeLocalEvent<TriggerOnMeleeSwingComponent, MeleeHitEvent>(OnSwingTrigger);
    }

    private void OnHitTrigger(Entity<TriggerOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            return;

        if (!ent.Comp.TriggerEveryHit)
        {
            var target = ent.Comp.TargetIsUser ? args.HitEntities.FirstOrDefault() : args.User;
            _trigger.Trigger(ent.Owner, target, ent.Comp.KeyOut);
            return;
        }

        // if TriggerEveryHit
        foreach (var target in args.HitEntities)
            _trigger.Trigger(ent.Owner, ent.Comp.TargetIsUser ? target : args.User, ent.Comp.KeyOut);
    }

    private void OnMissTrigger(Entity<TriggerOnMeleeMissComponent> ent, ref MeleeHitEvent args)
    {
        if (args.HitEntities.Count == 0)
            _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
    }

    private void OnSwingTrigger(Entity<TriggerOnMeleeSwingComponent> ent, ref MeleeHitEvent args)
    {
        var target = ent.Comp.TargetIsUser ? args.HitEntities.FirstOrDefault() : args.User;
        _trigger.Trigger(ent.Owner, target, ent.Comp.KeyOut);
    }
}
