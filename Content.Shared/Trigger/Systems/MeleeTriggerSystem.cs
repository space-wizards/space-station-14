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

        SubscribeLocalEvent<TriggerOnMeleeHitComponent, MeleeHitEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<TriggerOnMeleeHitComponent> ent, ref MeleeHitEvent args)
    {
        // Return if we're a "hit" mode and hit nothing
        if (args.HitEntities.Count == 0 && ent.Comp.Mode is TriggerOnMeleeHitMode.OnceOnHit or TriggerOnMeleeHitMode.EveryHit)
            return;

        if (args.HitEntities.Count == 0 && ent.Comp.Mode == TriggerOnMeleeHitMode.OnMiss)
        {
            // You missed! Hope this trigger doesn't do something bad to you.
            _trigger.Trigger(ent.Owner, args.User, ent.Comp.KeyOut);
            return;
        }

        // Single trigger modes
        if (ent.Comp.Mode is TriggerOnMeleeHitMode.OnSwing
                          or TriggerOnMeleeHitMode.OnceOnHit )
        {
            var target = ent.Comp.TargetIsUser ? args.HitEntities.FirstOrDefault() : args.User;
            _trigger.Trigger(ent.Owner, target, ent.Comp.KeyOut);
            return;
        }

        // if TriggerOnMeleeHitMode.EveryHit
        foreach (var target in args.HitEntities)
            _trigger.Trigger(ent.Owner, ent.Comp.TargetIsUser ? target : args.User, ent.Comp.KeyOut);
    }
}
