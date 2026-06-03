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

        if (TryComp<EntityTargetActionComponent>(ent, out var entityTarget) && entityTarget.Event is { } ev)
            target = ev.Target;

        if (HasComp<InstantActionComponent>(ent))
            target = args.Performer;

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

        _popup.PopupPredicted(selfMessage, othersMessage, args.Performer, args.Performer, ent.Comp.PopupType);

        if (target != null)
            _popup.PopupEntity(targetMessage, target.Value, target.Value, ent.Comp.PopupType);
    }
}
