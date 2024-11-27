using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Silicons.Laws.Components;
using Content.Shared.Tag;
using Content.Shared.Whitelist;

namespace Content.Shared.Emag.Systems;

/// How to add an emag interaction:
/// 1. Go to the system for the component you want the interaction with
/// 2. Subscribe to the GotEmaggedEvent
/// 3. Have some check for if this actually needs to be emagged or is already emagged (to stop charge waste)
/// 4. Past the check, add all the effects you desire and HANDLE THE EVENT ARGUMENT so a charge is spent
/// 5. Optionally, set Repeatable on the event to true if you don't want the emagged component to be added
public sealed class EmagSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!; // DeltaV - Add a whitelist/blacklist to the Emag
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EmagComponent, AfterInteractEvent>(OnAfterInteract);
    }

    private void OnAfterInteract(EntityUid uid, EmagComponent comp, AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryUseEmag(uid, args.User, target, comp);
    }

    /// <summary>
    /// Tries to use the emag on a target entity
    /// </summary>
    public bool TryUseEmag(EntityUid uid, EntityUid user, EntityUid target, EmagComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        if (_tag.HasTag(target, comp.EmagImmuneTag))
            return false;

            // DeltaV - Add a whitelist / blacklist to the Emag
        if (_whitelist.IsWhitelistFail(comp.Whitelist, target)
            || _whitelist.IsBlacklistPass(comp.Blacklist, target))
        {
            _popup.PopupClient(Loc.GetString("emag-invalid-target", ("emag", uid), ("target", target)), user, user);
            return false;
        }
            // End of DeltaV code

        TryComp<LimitedChargesComponent>(uid, out var charges);
        if (_charges.IsEmpty(uid, charges))
        {
            _popup.PopupClient(Loc.GetString("emag-no-charges"), user, user);
            return false;
        }

        var handled = DoEmagEffect(user, target);
        if (!handled)
            return false;

        _popup.PopupClient(Loc.GetString("emag-success", ("target", Identity.Entity(target, EntityManager))), user,
            user, PopupType.Medium);

        _adminLogger.Add(LogType.Emag, LogImpact.High, $"{ToPrettyString(user):player} emagged {ToPrettyString(target):target}");

        if (charges != null)
            _charges.UseCharge(uid, charges);
        return true;
    }

    /// <summary>
    /// Does the emag effect on a specified entity
    /// </summary>
    public bool DoEmagEffect(EntityUid user, EntityUid target)
    {
        // prevent emagging twice
        if (HasComp<EmaggedComponent>(target))
            return false;

        var onAttemptEmagEvent = new OnAttemptEmagEvent(user);
        RaiseLocalEvent(target, ref onAttemptEmagEvent);

        // prevent emagging if attempt fails
        if (onAttemptEmagEvent.Handled)
            return false;

        var emaggedEvent = new GotEmaggedEvent(user);
        RaiseLocalEvent(target, ref emaggedEvent);

        if (emaggedEvent.Handled && !emaggedEvent.Repeatable)
            EnsureComp<EmaggedComponent>(target);
        return emaggedEvent.Handled;
    }
}

/// <summary>
/// Shows a popup to emag user (client side only!) and adds <see cref="EmaggedComponent"/> to the entity when handled
/// </summary>
/// <param name="UserUid">Emag user</param>
/// <param name="Handled">Did the emagging succeed? Causes a user-only popup to show on client side</param>
/// <param name="Repeatable">Can the entity be emagged more than once? Prevents adding of <see cref="EmaggedComponent"/></param>
/// <remarks>Needs to be handled in shared/client, not just the server, to actually show the emagging popup</remarks>
[ByRefEvent]
public record struct GotEmaggedEvent(EntityUid UserUid, bool Handled = false, bool Repeatable = false);

[ByRefEvent]
public record struct OnAttemptEmagEvent(EntityUid UserUid, bool Handled = false);
