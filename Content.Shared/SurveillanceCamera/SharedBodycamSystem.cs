using System.Net.Mime;
using Content.Shared.Emp;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared.SurveillanceCamera;

/// <summary>
/// Manages the bodycams.
/// </summary>
public abstract class SharedBodycamSystem: EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodycamComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<BodycamComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<BodycamComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<BodycamComponent, ExaminedEvent>(OnBodycamExamine);
        SubscribeLocalEvent<BodycamComponent, InventoryRelayedEvent<ExaminedEvent>>(OnBodycamWearerExamine);
    }

    private void OnEquip(EntityUid uid, BodycamComponent comp, GotEquippedEvent args)
    {
        comp.Wearer = args.Equipee;
        Dirty(uid, comp);
    }

    private void OnUnequip(EntityUid uid, BodycamComponent comp, GotUnequippedEvent args)
    {
        comp.Wearer = null;
        Dirty(uid, comp);

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

    private void OnBodycamExamine(EntityUid uid, BodycamComponent comp, ExaminedEvent args)
    {
        if (comp.State == BodycamState.Active)
            args.PushMarkup(Loc.GetString("bodycam-examine-enabled"));
        else if (comp.State == BodycamState.Disabled)
            args.PushMarkup(Loc.GetString("bodycam-examine-disabled"));
    }

    private void OnBodycamWearerExamine(EntityUid uid, BodycamComponent comp, InventoryRelayedEvent<ExaminedEvent> args)
    {
        var identity = Identity.Entity(args.Args.Examined, EntityManager);
        if (comp.State == BodycamState.Active)
            args.Args.PushMarkup(Loc.GetString("bodycam-wearer-examine-enabled", ("identity", identity)));
        else if (comp.State == BodycamState.Disabled)
            args.Args.PushMarkup(Loc.GetString("bodycam-wearer-examine-disabled", ("identity", identity)));
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
        Dirty(uid, comp);

        // we assume the person wearing the bodycam is the one turning it on
        var identity = Identity.Entity(user, EntityManager);
        var name = Identity.Name(user, EntityManager);
        // what the person who switches the body cam on sees (only send to user client)
        _popup.PopupClient(Loc.GetString("bodycam-switch-on-message-self"), uid, user);
        // what everyone else sees (filter out the user from it)
        _popup.PopupEntity(Loc.GetString("bodycam-switch-on-message-other", ("user", name), ("identity", identity)), uid, Filter.Pvs(user, entityManager: EntityManager).RemoveWhere(e => e.AttachedEntity == user), true);
    }

    /// <summary>
    /// Called on the bodycam when someone alt-clicks it to turn it off or it gets taken off.
    /// Turns the bodycam off and disables the camera (which happens on the server version of this system).
    /// <param name="causedByPlayer">If the bodycam was swtiched off because the player did the verb or if it automatically switched off from being unequipped.</param>
    /// </summary>
    protected virtual void SwitchOff(EntityUid uid, BodycamComponent comp, EntityUid user, bool causedByPlayer)
    {
        comp.State = BodycamState.Disabled;
        Dirty(uid, comp);

        if (causedByPlayer)
        {
            // we assume the person wearing the bodycam is the one turning it off
            var identity = Identity.Entity(user, EntityManager);
            var name = Identity.Name(user, EntityManager);
            // what the person who switches the body cam on sees (only send to user client)
            _popup.PopupClient(Loc.GetString("bodycam-switch-off-message-self"), uid, user);
            // what everyone else sees (filter out the user from it)
            _popup.PopupEntity(Loc.GetString("bodycam-switch-off-message-other", ("user", name), ("identity", identity)), user, Filter.Pvs(user, entityManager: EntityManager).RemoveWhere(e => e.AttachedEntity == user), true);
        }
        else
        {
            _popup.PopupPredicted(Loc.GetString("bodycam-switch-off-message-unequipped"), user, user);
        }
    }
}
