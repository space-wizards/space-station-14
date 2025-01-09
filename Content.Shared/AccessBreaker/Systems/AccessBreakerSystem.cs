using Content.Shared.Administration.Logs;
using Content.Shared.Charges.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Database;
using Content.Shared.Emag.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Tag;

namespace Content.Shared.AccessBreaker;

public sealed class AccessBreakerSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private readonly SharedChargesSystem _charges = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly TagSystem _tag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AccessBreakerComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<AccessBrokenComponent, OnAccessOverriderAccessUpdatedEvent>(OnAccessFixed);
    }

    private void OnAfterInteract(Entity<AccessBreakerComponent> ent, ref AfterInteractEvent args)
    {
        if (!args.CanReach || args.Target is not { } target)
            return;

        args.Handled = TryBreakAccess(ent, args.User, target, ent.Comp);
    }

    private void OnAccessFixed(Entity<AccessBrokenComponent> ent, ref OnAccessOverriderAccessUpdatedEvent args)
    {
        RemCompDeferred<AccessBrokenComponent>(ent);

        args.Handled = true;
    }

    /// <summary>
    /// Tries to break the access/lock on a target entity
    /// </summary>
    public bool TryBreakAccess(EntityUid uid, EntityUid user, EntityUid target, AccessBreakerComponent? comp = null)
    {
        if (!Resolve(uid, ref comp, false))
            return false;

        if (_tag.HasTag(target, comp.AccessBreakerImmuneTag))
            return false;

        if (comp.LastTarget == target)
            return false;

        TryComp<LimitedChargesComponent>(uid, out var charges);
        if (_charges.IsEmpty(uid, charges))
        {
            _popup.PopupClient(Loc.GetString("emag-no-charges"), user, user);
            return false;
        }

        var handled = DoAccessBreakerEffect(user, target);
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
    /// Does the access breaker effect on a specified entity
    /// </summary>
    public bool DoAccessBreakerEffect(EntityUid user, EntityUid target)
    {
        if (HasComp<AccessBrokenComponent>(target))
            return false;

        var onAttemptAccessBreakEvent = new OnAttemptAccessBreakEvent(user);
        RaiseLocalEvent(target, ref onAttemptAccessBreakEvent);

        // prevent breaking if attempt fails
        if (onAttemptAccessBreakEvent.Handled)
            return false;

        var accessBrokenEvent = new GotAccessBrokenEvent(user);
        RaiseLocalEvent(target, ref accessBrokenEvent);

        if (accessBrokenEvent.Handled)
            EnsureComp<AccessBrokenComponent>(target);
        return accessBrokenEvent.Handled;
    }
}

/// <summary>
/// Shows a popup to emag user (client side only!) and adds <see cref="EmaggedComponent"/> to the entity when handled
/// </summary>
/// <param name="UserUid">Emag user</param>
/// <param name="Handled">Did the emagging succeed? Causes a user-only popup to show on client side</param>
/// <remarks>Needs to be handled in shared/client, not just the server, to actually show the emagging popup</remarks>
[ByRefEvent]
public record struct GotAccessBrokenEvent(EntityUid UserUid, bool Handled = false);

[ByRefEvent]
public record struct OnAttemptAccessBreakEvent(EntityUid UserUid, bool Handled = false);
