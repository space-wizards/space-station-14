using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.EntityEffects;

namespace Content.Shared.Actions;

/// <summary>
/// Handles <see cref="PopupOnActionComponent"/>.
/// </summary>
public sealed partial class PopupOnActionSystem : EntitySystem
{
    [Dependency] private SharedEntityEffectsSystem _effects = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<PopupOnActionComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnActionPerformed(Entity<PopupOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        EntityUid popupTarget;

        if (ent.Comp.Recipient == PopupRecipient.Target
            && TryComp<EntityTargetActionComponent>(ent, out var entityTarget)
            && entityTarget.Event is {} ev)
        {
            popupTarget = ev.Target;
        }
        else
        {
            popupTarget = args.Performer;
        }

        _effects.ApplyEffect(popupTarget, ent.Comp.Popup);
    }
}
