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
