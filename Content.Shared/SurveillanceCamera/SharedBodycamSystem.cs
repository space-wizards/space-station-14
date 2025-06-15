using System.Net.Mime;
using Content.Shared.Emp;
using Content.Shared.EntityEffects.Effects;
using Content.Shared.Examine;
using Content.Shared.IdentityManagement;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Popups;
using Content.Shared.PowerCell;
using Content.Shared.PowerCell.Components;
using Content.Shared.Verbs;
using Robust.Shared.Player;

namespace Content.Shared.SurveillanceCamera;

/// <summary>
/// Manages the bodycams.
/// </summary>
public abstract class SharedBodycamSystem: EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedPowerCellSystem _cell = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<BodycamComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<BodycamComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<BodycamComponent, GotUnequippedEvent>(OnUnequip);
        SubscribeLocalEvent<BodycamComponent, ExaminedEvent>(OnBodycamExamine);
        SubscribeLocalEvent<BodycamComponent, InventoryRelayedEvent<ExaminedEvent>>(OnBodycamWearerExamine);
        SubscribeLocalEvent<BodycamComponent, PowerCellSlotEmptyEvent>(OnCellEmpty);
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
            SwitchOff(uid, comp, null, args.Equipee);
    }

    private void OnGetVerbs(EntityUid uid, BodycamComponent comp, GetVerbsEvent<AlternativeVerb> args)
    {
        // you don't get to see the verbs if someone else is wearing it
        if (comp.Wearer is not null && comp.Wearer != args.User)
            return;

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
            else if (HasComp<PowerCellDrawComponent>(uid) && !_cell.HasDrawCharge(uid))
            {
                disabled = true;
                message = Loc.GetString("bodycam-switch-on-verb-disabled-no-power");
            }

            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => SwitchOn(uid, comp, args.User),
                Text = Loc.GetString("bodycam-switch-on-verb"),
                Disabled = disabled,
                Message = message,
                Priority = disabled ? -1 : 1
            });
        }

        if (comp.State == BodycamState.Active)
        {
            args.Verbs.Add(new AlternativeVerb()
            {
                Act = () => SwitchOff(uid, comp, args.User),
                Text = Loc.GetString("bodycam-switch-off-verb"),
                Message = Loc.GetString("bodycam-switch-off-verb-tooltip"),
                Priority = 1
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

    private void OnCellEmpty(EntityUid uid, BodycamComponent comp, ref PowerCellSlotEmptyEvent args)
    {
        if (comp.State == BodycamState.Active)
            SwitchOff(uid, comp);
    }

    /// <summary>
    /// Called on a bodycam when someone alt-clicks it to turn it on.
    /// Turns the bodycam on and enables the camera (which happens on the server version of this system).
    /// <returns>Returns true if it managed to switch on successfully.</returns>
    /// </summary>
    protected virtual bool SwitchOn(EntityUid uid, BodycamComponent comp, EntityUid user)
    {
        // if no one is wearing it you can't switch it on
        // if someone is wearing it but it isn't you, you don't get to switch it on
        // if it is disabled by an emp you don't get to switch it on
        // if it needs power but is out of power you don't get to switch it on
        if (comp.Wearer is null || comp.Wearer != user || HasComp<EmpDisabledComponent>(uid) || HasComp<PowerCellDrawComponent>(uid) && !_cell.HasActivatableCharge(uid))
            return false;

        comp.State = BodycamState.Active;
        Dirty(uid, comp);

        // shame there isnt method which handles turning on a powercell :(
        if (TryComp<PowerCellDrawComponent>(uid, out var powerCell))
        {
            powerCell.Enabled = true;
            Dirty(uid, powerCell);
        }

        // we assume the person wearing the bodycam is the one turning it on
        var identity = Identity.Entity(user, EntityManager);
        var name = Identity.Name(user, EntityManager);
        // what the person who switches the body cam on sees (only send to user client)
        _popup.PopupClient(Loc.GetString("bodycam-switch-on-message-self"), uid, user);
        // what everyone else sees (filter out the user from it)
        _popup.PopupEntity(Loc.GetString("bodycam-switch-on-message-other", ("user", name), ("identity", identity)), uid, Filter.Pvs(user, entityManager: EntityManager).RemoveWhere(e => e.AttachedEntity == user), true);

        return true;
    }

    /// <summary>
    /// Called on the bodycam when someone alt-clicks it to turn it off or it gets taken off.
    /// Turns the bodycam off and disables the camera (which happens on the server version of this system).
    /// <param name="user">The entity that switched off the bodycam, if applicable.</param>
    /// <param name="unequipper">The entity that caused the bodycam to automatically shut off by removing it from someone, if applicable.</param>
    /// <returns>Returns true if it managed to switch off successfully.</returns>
    /// </summary>
    protected virtual bool SwitchOff(EntityUid uid, BodycamComponent comp, EntityUid? user = null, EntityUid? unequipper = null)
    {
        // you can't switch it off if someone is wearing it and you aren't
        if (comp.Wearer is not null && user is not null && comp.Wearer != user)
            return false;

        comp.State = BodycamState.Disabled;
        Dirty(uid, comp);

        if (TryComp<PowerCellDrawComponent>(uid, out var powerCell))
        {
            powerCell.Enabled = false;
            Dirty(uid, powerCell);
        }

        if (user is not null)
        {
            // we assume the person wearing the bodycam is the one turning it off
            var identity = Identity.Entity(user.Value, EntityManager);
            var name = Identity.Name(user.Value, EntityManager);
            // what the person who switches the body cam on sees (only send to user client)
            _popup.PopupClient(Loc.GetString("bodycam-switch-off-message-self"), uid, user);
            // what everyone else sees (filter out the user from it)
            _popup.PopupEntity(Loc.GetString("bodycam-switch-off-message-other", ("user", name), ("identity", identity)), user.Value, Filter.Pvs(user.Value, entityManager: EntityManager).RemoveWhere(e => e.AttachedEntity == user.Value), true);
        }
        else if (unequipper is not null)
        {
            _popup.PopupPredicted(Loc.GetString("bodycam-switch-off-message-unequipped"), uid, unequipper);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("bodycam-switch-off-message-unequipped"), uid);
        }

        return true;
    }
}
