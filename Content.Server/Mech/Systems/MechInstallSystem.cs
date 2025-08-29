using Content.Server.Popups;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Whitelist;
using Content.Shared.Vehicle;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Base system helpers to handle installing items (equipment/modules) into mechs without generics.
/// Derived systems subscribe to events and use these helpers.
/// </summary>
public abstract class MechInstallSystem : EntitySystem
{
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected readonly PopupSystem Popup = default!;
    [Dependency] protected readonly EntityWhitelistSystem Whitelist = default!;
    [Dependency] protected readonly VehicleSystem Vehicle = default!;
    [Dependency] protected readonly SharedMechSystem MechSystem = default!;

    /// <summary>
    /// Common precondition checks before starting install. Validates mech, broken/closed states and actor relation.
    /// </summary>
    protected bool TryPrepareInstall(EntityUid item, EntityUid user, EntityUid target, out MechComponent? mechComp)
    {
        mechComp = default!;

        if (!TryComp<MechComponent>(target, out mechComp))
            return false;

        // Block install if mech is in broken state
        if (mechComp.Broken && !Vehicle.HasOperator(target))
        {
            Popup.PopupEntity(Loc.GetString("mech-cannot-insert-broken"), user);
            return false;
        }

        // Block install if cabin is closed
        if (Vehicle.HasOperator(target))
        {
            Popup.PopupEntity(Loc.GetString("mech-cannot-modify-closed"), user);
            return false;
        }

        if (user == Vehicle.GetOperatorOrNull(target))
            return false;

        return true;
    }

    /// <summary>
    /// Checks duplicate by prototype id among already installed items. Pops up on duplicate.
    /// </summary>
    protected bool HasDuplicateInstalled(EntityUid item, IReadOnlyList<EntityUid> installed, EntityUid user)
    {
        var md = EntityManager.GetComponentOrNull<MetaDataComponent>(item);
        if (md?.EntityPrototype == null)
            return false;

        var id = md.EntityPrototype.ID;
        foreach (var ent in installed)
        {
            var md2 = EntityManager.GetComponentOrNull<MetaDataComponent>(ent);
            if (md2?.EntityPrototype != null && md2.EntityPrototype.ID == id)
            {
                Popup.PopupEntity(Loc.GetString("mech-duplicate-installed-popup"), user);
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Starts the install do-after with provided event.
    /// </summary>
    protected void StartInstallDoAfter(EntityUid user, EntityUid item, EntityUid mech, float duration, SimpleDoAfterEvent insertEvent)
    {
        Popup.PopupEntity(Loc.GetString("mech-install-begin-popup", ("item", item)), mech);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, duration, insertEvent, item, target: mech, used: item)
        {
            BreakOnMove = true,
        };

        DoAfter.TryStartDoAfter(doAfterEventArgs);
    }

    /// <summary>
    /// Shared finalization checks before performing insert (locks and mech existence). Pops up on denial.
    /// </summary>
    protected bool TryFinalizeInsert(EntityUid mech, EntityUid user, out MechComponent? mechComp)
    {
        mechComp = default!;

        if (TryComp<MechLockComponent>(mech, out var lockComp) && lockComp.IsLocked)
        {
            var lockSys = EntityManager.System<MechLockSystem>();
            if (!lockSys.CheckAccessWithFeedback(mech, user, lockComp))
                return false;
        }

        if (!TryComp<MechComponent>(mech, out mechComp))
            return false;

        return true;
    }

    /// <summary>
    /// Pops up standard finish message.
    /// </summary>
    protected void PopupFinish(EntityUid mech, EntityUid item)
    {
        Popup.PopupEntity(Loc.GetString("mech-install-finish-popup", ("item", item)), mech);
    }
}
