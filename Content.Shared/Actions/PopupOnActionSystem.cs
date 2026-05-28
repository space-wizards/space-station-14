using Content.Shared.Actions.Components;
using Content.Shared.Actions.Events;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

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

        if (TryComp<EntityTargetActionComponent>(ent, out var entityTarget)
            && entityTarget.Event is { } ev)
        {
            target = ev.Target;
        }

        var user = args.Performer;
        var userName = Identity.Name(user, EntityManager);
        var targetName = target != null ? Identity.Name(target.Value, EntityManager) : string.Empty;

        if (ent.Comp.UserMessage != null)
            ShowPopup(ent.Comp.UserMessage, user, user, userName, targetName, ent.Comp.PopupType);

        if (ent.Comp.TargetMessage != null && target != null)
            ShowPopup(ent.Comp.TargetMessage, target.Value, target.Value, userName, targetName, ent.Comp.PopupType);
    }

    private void ShowPopup(PopupMessage popup, EntityUid uid, EntityUid recipient, string userName, string targetName, PopupType type)
    {
        var message = popup.Message;
        if (string.IsNullOrEmpty(message))
            return;

        var text = Loc.GetString(message, ("user", userName), ("target", targetName));
        if (popup.Recipients == PopupRecipients.Local)
            _popup.PopupClient(text, uid, recipient, type);
        else
            _popup.PopupPredicted(text, uid, recipient, type);
    }
}
