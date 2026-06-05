using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;

namespace Content.Shared.Actions;

/// <summary>
/// Handles applying entity effects when an entity effect action is performed.
/// </summary>
public sealed partial class EntityEffectActionSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<EntityEffectActionComponent, EntityEffectActionEvent>(OnEntityEffectAction);
        SubscribeLocalEvent<EntityEffectActionComponent, EntityEffectInstantActionEvent>(OnEntityEffectInstantAction);
    }

    private void OnEntityEffectInstantAction(Entity<EntityEffectActionComponent> ent, ref EntityEffectInstantActionEvent args)
    {
        foreach (var effect in ent.Comp.Effects)
        {
            // we trigger the actions on the user
            if (_effects.TryApplyEffect(args.Performer, effect, user: args.Performer))
                args.Handled = true;
        }
    }

    private void OnEntityEffectAction(Entity<EntityEffectActionComponent> ent, ref EntityEffectActionEvent args)
    {
        foreach (var effect in ent.Comp.Effects)
        {
            if (_effects.TryApplyEffect(args.Target, effect, user: args.Performer))
                args.Handled = true;
        }
    }
}
