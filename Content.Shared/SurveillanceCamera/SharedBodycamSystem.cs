using Content.Shared.Emp;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared.SurveillanceCamera;

public abstract class SharedBodycamSystem: EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodycamComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<BodycamComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<BodycamComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<BodycamComponent, ExaminedEvent>(OnExamine);
    }

    private void OnEquip(EntityUid uid, BodycamComponent comp, GotEquippedEvent args)
    {
        comp.Wearer = args.Equipee;
    }

    private void OnUnequip(EntityUid uid, BodycamComponent comp, GotUnequippedEvent args)
    {
        comp.Wearer = null;

        if (comp.State == BodycamState.Active)
            SwitchOff(uid, comp, args.Equipee, false);
    }

    private void OnGetVerbs(EntityUid uid, BodycamComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        if (comp.State == BodycamState.Disabled)
        {
            var disabled = false;
            var message = Loc.GetString("bodycam-switch-on-verb-tooltip");
            if (comp.Wearer is null)
            {
                disabled = true;
                message = Loc.GetString("bodycam-switch-on-verb-disabled-unequipped");
            }
            else if (HasComp<EmpDisabledComponent>(uid))
            {
                disabled = true;
                message = Loc.GetString("bodycam-switch-on-verb-disabled-emp");
            }

            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => SwitchOn(uid, comp, args.User),
                Text = Loc.GetString("bodycam-switch-on-verb"),
                Disabled = disabled,
                Message = message
            });
        }

        if (comp.State == BodycamState.Active)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => SwitchOff(uid, comp, args.User, true),
                Text = Loc.GetString("bodycam-switch-off-verb"),
                Message = Loc.GetString("bodycam-switch-off-verb-tooltip")
            });
        }
    }

    private void OnExamine(EntityUid uid, BodycamComponent comp, ExaminedEvent args)
    {
        if (comp.State == BodycamState.Active)
            args.PushMarkup(Loc.GetString("bodycam-examine-enabled"));
        else if (comp.State == BodycamState.Disabled)
            args.PushMarkup(Loc.GetString("bodycam-examine-disabled"));
    }

    /// <summary>
    /// Called on a bodycam when someone alt-clicks it to turn it on.
    /// Turns the bodycam on and enables the camera (which happens on the server version of this system).
    /// </summary>
    protected virtual void SwitchOn(EntityUid uid, BodycamComponent comp, EntityUid user)
    {
        if (comp.Wearer is null || HasComp<EmpDisabledComponent>(uid))
            return;

        comp.State = BodycamState.Active;

        // what the person who switches the body cam on sees (only send to user client)
        _popup.PopupClient(Loc.GetString("bodycam-switch-on-message-self"), user, user);
        // what everyone else sees (filter out the user from it)
        _popup.PopupEntity(Loc.GetString("bodycam-switch-on-message-other", ("user", Identity.Name(user, EntityManager))), user, Filter.Pvs(user, entityManager: EntityManager).RemoveWhere(e => e.AttachedEntity == user), true);
    }

    /// <summary>
    /// Called on the bodycam when someoen alt-clicks it to turn it off or it gets taken off.
    /// Turns the bodycam off and disables the camera (which happens on the server version of this system).
    /// <param name="causedByPlayer">If the bodycam was swtiched off because the player did the verb or if it automatically switched off from being unequipped.</param>
    /// </summary>
    protected virtual void SwitchOff(EntityUid uid, BodycamComponent comp, EntityUid user, bool causedByPlayer)
    {
        comp.State = BodycamState.Disabled;

        if (causedByPlayer)
        {
            // what the person who switches the body cam on sees (only send to user client)
            _popup.PopupClient(Loc.GetString("bodycam-switch-off-message-self"), user, user);
            // what everyone else sees (filter out the user from it)
            _popup.PopupEntity(Loc.GetString("bodycam-switch-off-message-other", ("user", Identity.Name(user, EntityManager))), user, Filter.Pvs(user, entityManager: EntityManager).RemoveWhere(e => e.AttachedEntity == user), true);
        }
        else
        {
            _popup.PopupPredicted(Loc.GetString("bodycam-switch-off-message-unequipped"), user, user);
        }
    }
}
