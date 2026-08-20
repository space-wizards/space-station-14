using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;

namespace Content.Shared.Actions;

/// <summary>
/// Handles applying entity effects when an entity effect action is performed.
/// </summary>
public sealed partial class EntityEffectActionSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    [SubscribeLocalEvent]
    private void OnEntityEffectInstantAction(Entity<EntityEffectActionComponent> ent, ref EntityEffectInstantActionEvent args)
    {
        if (ent.Comp.Effects == null)
            return;
        
        // we trigger the actions on the user
        if (_effects.TryApplyEffects(args.Performer, ent.Comp.Effects, user: args.Performer))
            args.Handled = true;
    }

    [SubscribeLocalEvent]
    private void OnEntityEffectAction(Entity<EntityEffectActionComponent> ent, ref EntityEffectActionEvent args)
    {
        if (ent.Comp.Effects == null)
            return;

        if (_effects.TryApplyEffects(args.Target, ent.Comp.Effects, user: args.Performer))
            args.Handled = true;
        
    }
}
