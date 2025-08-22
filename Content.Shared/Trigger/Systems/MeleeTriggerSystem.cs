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
        if (args.HitEntities.Count == 0 && ent.Comp.Mode != TriggerOnMeleeHitMode.OnceOnSwing)
            return;

        if (ent.Comp.Mode is TriggerOnMeleeHitMode.OnceOnSwing
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
