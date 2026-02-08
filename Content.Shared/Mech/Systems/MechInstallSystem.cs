using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Mech.Components;
using Content.Shared.Popups;
using Content.Shared.Vehicle;
using Content.Shared.Whitelist;

namespace Content.Shared.Mech.Systems;

/// <summary>
/// Provides shared helper logic for installing mech equipment and modules.
/// Intended to be inherited by specific mech install systems.
/// </summary>
public abstract class MechInstallSystem : EntitySystem
{
    [Dependency] protected readonly EntityWhitelistSystem Whitelist = default!;
    [Dependency] protected readonly SharedDoAfterSystem DoAfter = default!;
    [Dependency] protected readonly SharedMechSystem Mech = default!;
    [Dependency] protected readonly MechLockSystem MechLock = default!;
    [Dependency] protected readonly SharedPopupSystem Popup = default!;
    [Dependency] protected readonly VehicleSystem Vehicle = default!;

    /// <summary>
    /// Common precondition checks before starting install. Validates mech, broken/closed states and actor relation.
    /// </summary>
    protected bool TryPrepareInstall(EntityUid user, EntityUid target, out MechComponent? mechComp)
    {
        if (!TryComp(target, out mechComp))
            return false;

        // Check lock access before starting install.
        if (!MechLock.CheckAccessWithFeedback(target, user))
            return false;

        // Block install if mech is in broken state.
        if (mechComp.Broken && !Vehicle.HasOperator(target))
        {
            Popup.PopupClient(Loc.GetString("mech-cannot-insert-broken-popup"), user, user);
            return false;
        }

        // Block install if cabin is closed.
        if (Vehicle.HasOperator(target))
        {
            Popup.PopupClient(Loc.GetString("mech-cannot-modify-closed-popup"), user, user);
            return false;
        }

        if (user == Vehicle.GetOperatorOrNull(target))
            return false;

        return true;
    }

    /// <summary>
    /// Checks duplicate by prototype id among already installed items.
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
                Popup.PopupClient(Loc.GetString("mech-duplicate-installed-popup"), user, user);
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
        Popup.PopupPredicted(Loc.GetString("mech-install-begin-popup", ("user", Identity.Entity(user, EntityManager)), ("item", item)), user, user);

        var doAfterEventArgs = new DoAfterArgs(EntityManager, user, duration, insertEvent, item, target: mech, used: item)
        {
            BreakOnMove = true,
        };

        DoAfter.TryStartDoAfter(doAfterEventArgs);
    }
}
