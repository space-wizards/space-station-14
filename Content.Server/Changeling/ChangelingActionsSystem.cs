using Content.Shared.Actions;
using Content.Shared.Changeling;
using Content.Shared.IdentityManagement;
using Content.Shared.Popups;

namespace Content.Server.Changeling;

public sealed partial class ChangelingActionsSystem : EntitySystem
{
    [Dependency] private readonly ChangelingSystem _changeling = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChangelingComponent, ChangelingActionEvent>(OnActionPerformed);
        SubscribeLocalEvent<ChangelingComponent, ChangelingStingEvent>(OnSting);
    }

    private bool OnAction(Entity<ChangelingComponent> ent, BaseActionEvent args)
    {
        if (args.Handled)
            return false;

        if (!TryComp<ChangelingActionComponent>(ent, out var actionComp))
            return false;

        if (!TryAction(ent, ent.Comp, actionComp))
            return false;

        if (actionComp.Event != null)
        {
            // that is assuming the event is derived from InstantActionEvent
            RaiseLocalEvent(actionComp.Event);
        }

        if (actionComp.Audible)
            _changeling.PlayMeatySound(ent, ent.Comp);

        return true;
    }

    private void OnActionPerformed(Entity<ChangelingComponent> ent, ref ChangelingActionEvent args)
    {
        if (!OnAction(ent, args))
            return;
        args.Handled = true;
    }

    private void OnSting(Entity<ChangelingComponent> ent, ref ChangelingStingEvent args)
    {
        if (!TrySting(ent, args.Target))
            return;

        if (!OnAction(ent, args))
            return;
        args.Handled = true;
    }

    public bool TryAction(EntityUid uid, ChangelingComponent? lingComp, ChangelingActionComponent actionComp)
    {
        if (!Resolve(uid, ref lingComp))
            return false;

        if (!actionComp.UseWhileLesserForm && lingComp.IsInLesserForm)
        {
            _popup.PopupEntity(Loc.GetString("changeling-action-fail-lesserform"), uid, uid);
            return false;
        }

        if (lingComp.Chemicals < actionComp.ChemicalCost)
        {
            _popup.PopupEntity(Loc.GetString("changeling-chemicals-deficit"), uid, uid);
            return false;
        }

        if (lingComp.TotalAbsorbedEntities < actionComp.RequireAbsorbed)
        {
            var delta = actionComp.RequireAbsorbed - lingComp.TotalAbsorbedEntities;
            _popup.PopupEntity(Loc.GetString("changeling-action-fail-absorbed", ("number", delta)), uid, uid);
            return false;
        }

        _changeling.UpdateChemicals(uid, lingComp, -actionComp.ChemicalCost);

        return true;
    }

    /// <summary>
    ///     Try to silently sting a target. If a target is a changeling, notify both.
    /// </summary>
    public bool TrySting(EntityUid uid, EntityUid target)
    {
        if (HasComp<ChangelingComponent>(target))
        {
            var selfMessage = Loc.GetString("changeling-sting-fail-self", ("target", Identity.Entity(target, EntityManager)));
            var targetMessage = Loc.GetString("changeling-sting-fail-ling");

            _popup.PopupEntity(selfMessage, uid, uid);
            _popup.PopupEntity(targetMessage, target, target);
            return false;
        }
        _popup.PopupEntity(Loc.GetString("changeling-sting", ("target", Identity.Entity(target, EntityManager))), uid, uid);
        return true;
    }
}
