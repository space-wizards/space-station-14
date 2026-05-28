using Content.Shared.Actions.Components;
using Content.Shared.EntityEffects;

namespace Content.Shared.Actions;

/// <summary>
/// Handles <see cref="PopupOnActionComponent"/>.
/// </summary>
public sealed partial class StatusEffectActionSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<StatusEffectActionComponent, StatusEffectActionEvent>(OnStatusEffectAction);
    }

    private void OnStatusEffectAction(Entity<StatusEffectActionComponent> ent, ref StatusEffectActionEvent args)
    {
        var handled = false;
        foreach (var effect in ent.Comp.Effects)
        {
            if (_effects.TryApplyEffect(args.Target, effect))
                handled = true;
        }

        if (handled)
            args.Handled = true;
    }
}
