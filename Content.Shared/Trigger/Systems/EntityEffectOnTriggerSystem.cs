using Content.Shared.EntityEffects;
using Content.Shared.Trigger.Components.Effects;

namespace Content.Shared.Trigger.Systems;

public sealed class EntityEffectOnTriggerSystem : EntitySystem
{
    [Dependency] private readonly SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EntityEffectOnTriggerComponent, TriggerEvent>(OnTrigger);
    }

    private void OnTrigger(Entity<EntityEffectOnTriggerComponent> ent, ref TriggerEvent args)
    {
        if (args.Key != null && !ent.Comp.KeysIn.Contains(args.Key))
            return;

        var target = ent.Comp.TargetUser ? args.User : ent.Owner;

        if (target == null)
            return;

        _effects.ApplyEffects(target.Value, ent.Comp.Effects, ent.Comp.Scale);
        args.Handled = true;
    }
}
