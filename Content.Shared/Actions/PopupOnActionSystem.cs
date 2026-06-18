using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Shared.Actions;

/// <summary>
/// Handles displaying popup messages when actions are successfully performed.
/// </summary>
public sealed partial class PopupOnActionSystem : EntitySystem
{
    [Dependency] private SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<PopupOnActionComponent, ActionPerformedEvent>(OnActionPerformed);
    }

    private void OnActionPerformed(Entity<PopupOnActionComponent> ent, ref ActionPerformedEvent args)
    {
        EntityUid? target = null;

        if (TryComp<EntityTargetActionComponent>(ent, out var entityTarget) && entityTarget.Event is { } ev)
            target = ev.Target;

        var userName = Identity.Name(args.Performer, EntityManager);
        var targetName = target != null ? Identity.Name(target.Value, EntityManager) : string.Empty;

        var selfMessage = ent.Comp.SelfMessage != null
            ? Loc.GetString(ent.Comp.SelfMessage, ("target", targetName), ("user", userName))
            : null;

        var othersMessage = ent.Comp.OthersMessage != null
            ? Loc.GetString(ent.Comp.OthersMessage, ("target", targetName), ("user", userName))
            : null;

        var targetMessage = ent.Comp.TargetMessage != null
            ? Loc.GetString(ent.Comp.TargetMessage, ("target", targetName), ("user", userName))
            : null;

        // Popup to show to the performer.
        // If there is a target the popup is located on the target, if there is no target it is located on the user.
        _popup.PopupClient(selfMessage, target ?? args.Performer, args.Performer, ent.Comp.PopupType);

        // Popup to show to the target.
        // Located on the target.
        if (target != null)
            _popup.PopupEntity(targetMessage, target.Value, target.Value, ent.Comp.PopupType);

        // Popup for everyone else.
        // Located on the performer.
        var filter = Filter.PvsExcept(args.Performer, entityManager: EntityManager);
        if (target != null)
            filter = filter.RemovePlayerByAttachedEntity(target.Value);

        _popup.PopupEntity(othersMessage, args.Performer, filter, true, ent.Comp.PopupType);
    }
}
