using System.Linq;
using Content.Shared.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Popups;
using Content.Shared.Emag.Systems;
using Robust.Shared.Audio.Systems;

namespace Content.Shared.Mech.Systems;

/// <summary>
/// System responsible for handling mech locking logic.
/// </summary>
public sealed partial class MechLockSystem : EntitySystem
{
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechLockComponent, MechDnaLockRegisterEvent>(OnDnaLockRegister);
        SubscribeLocalEvent<MechLockComponent, MechDnaLockToggleEvent>(OnDnaLockToggle);
        SubscribeLocalEvent<MechLockComponent, MechDnaLockResetEvent>(OnDnaLockReset);
        SubscribeLocalEvent<MechLockComponent, MechCardLockRegisterEvent>(OnCardLockRegister);
        SubscribeLocalEvent<MechLockComponent, MechCardLockToggleEvent>(OnCardLockToggle);
        SubscribeLocalEvent<MechLockComponent, MechCardLockResetEvent>(OnCardLockReset);

        SubscribeLocalEvent<MechLockComponent, ComponentStartup>(OnLockStartup);
        SubscribeLocalEvent<MechLockComponent, GotEmaggedEvent>(OnEmagged);
    }

    /// <summary>
    /// Handles DNA lock registration
    /// </summary>
    private void OnDnaLockRegister(EntityUid uid, MechLockComponent component, MechDnaLockRegisterEvent args)
    {
        var user = GetEntity(args.User);
        if (user == EntityUid.Invalid)
            return;

        TryRegisterLock(uid, user, MechLockType.Dna, component);
    }

    /// <summary>
    /// Handles DNA lock toggle
    /// </summary>
    private void OnDnaLockToggle(EntityUid uid, MechLockComponent component, MechDnaLockToggleEvent args)
    {
        var user = GetEntity(args.User);
        if (user == EntityUid.Invalid)
            return;

        if (TryToggleLock(uid, MechLockType.Dna, component))
        {
            var (_, isActive, _) = GetLockState(MechLockType.Dna, component);
            ShowLockMessage(uid, user, isActive);
        }
    }

    /// <summary>
    /// Handles DNA lock reset
    /// </summary>
    private void OnDnaLockReset(EntityUid uid, MechLockComponent component, MechDnaLockResetEvent args)
    {
        var user = GetEntity(args.User);
        if (user == EntityUid.Invalid)
            return;

        TryResetLock(uid, user, MechLockType.Dna, component);
    }

    /// <summary>
    /// Handles card lock registration
    /// </summary>
    private void OnCardLockRegister(EntityUid uid, MechLockComponent component, MechCardLockRegisterEvent args)
    {
        var user = GetEntity(args.User);
        if (user == EntityUid.Invalid)
            return;

        TryRegisterLock(uid, user, MechLockType.Card, component);
    }

    /// <summary>
    /// Handles card lock toggle
    /// </summary>
    private void OnCardLockToggle(EntityUid uid, MechLockComponent component, MechCardLockToggleEvent args)
    {
        var user = GetEntity(args.User);
        if (user == EntityUid.Invalid)
            return;

        if (TryToggleLock(uid, MechLockType.Card, component))
        {
            var (_, isActive, _) = GetLockState(MechLockType.Card, component);
            ShowLockMessage(uid, user, isActive);
        }
    }

    /// <summary>
    /// Handles card lock reset
    /// </summary>
    private void OnCardLockReset(EntityUid uid, MechLockComponent component, MechCardLockResetEvent args)
    {
        var user = GetEntity(args.User);
        if (user == EntityUid.Invalid)
            return;

        TryResetLock(uid, user, MechLockType.Card, component);
    }

    private void UpdateMechUI(EntityUid uid)
    {
        var ev = new UpdateMechUiEvent();
        RaiseLocalEvent(uid, ev);
    }

    private bool TryFindIdCard(EntityUid user, out Entity<IdCardComponent> idCard)
    {
        return _idCard.TryFindIdCard(user, out idCard);
    }

    /// <summary>
    /// Checks if a user has access to a locked mech and can manage locks
    /// </summary>
    private bool HasAccess(EntityUid user, MechLockComponent component)
    {
        // If no locks are registered, access is granted
        if (!component.DnaLockRegistered && !component.CardLockRegistered)
        {
            return true;
        }

        // If any locks are registered, user must be registered for at least one type
        var isRegisteredForAny = IsAnyOwner(user, component);
        if (!isRegisteredForAny)
        {
            return false;
        }

        // If no locks are active, access is granted (but only for registered users)
        if (!component.DnaLockActive && !component.CardLockActive)
        {
            return true;
        }

        var hasAccess = false;

        // Check DNA lock - if active, user must have matching DNA
        if (component.DnaLockActive && component.DnaLockRegistered)
        {
            if (TryComp<DnaComponent>(user, out var dnaComp) && dnaComp.DNA == component.OwnerDna)
            {
                hasAccess = true;
            }
        }

        // Check card lock - if active, user must have matching access tags
        if (component.CardLockActive && component.CardLockRegistered)
        {
            if (TryFindIdCard(user, out var idCard))
            {
                if (TryComp<AccessComponent>(idCard.Owner, out var access) && access != null && access.Tags != null)
                {
                    var tags = access.Tags;
                    foreach (var tag in tags)
                    {
                        if (component.CardAccessTags!.Contains(tag))
                        {
                            hasAccess = true;
                            break;
                        }
                    }
                }
            }
        }

        // User has access if they can access at least one active lock type
        return hasAccess;
    }

    private bool IsOwnerOfLock(EntityUid user, MechLockType lockType, MechLockComponent component)
    {
        var (isRegistered, _, ownerId) = GetLockState(lockType, component);
        if (!isRegistered || ownerId == null)
            return false;
        switch (lockType)
        {
            case MechLockType.Dna:
                return TryComp<DnaComponent>(user, out var dnaComp) && dnaComp.DNA == ownerId;
            case MechLockType.Card:
                if (component.CardAccessTags == null || component.CardAccessTags.Count == 0)
                    return false;
                if (!TryFindIdCard(user, out var idCard))
                    return false;
                if (!TryComp<AccessComponent>(idCard.Owner, out var access) || access == null || access.Tags == null)
                    return false;

                var tags = access.Tags;

                return component.CardAccessTags.Any(tag => tags.Contains(tag));
        }
        return false;
    }

    private bool IsAnyOwner(EntityUid user, MechLockComponent component)
    {
        return IsOwnerOfLock(user, MechLockType.Dna, component) || IsOwnerOfLock(user, MechLockType.Card, component);
    }

    private void OnLockStartup(EntityUid uid, MechLockComponent component, ComponentStartup args)
    {
        UpdateLockState(uid, component);
    }

    /// <summary>
    /// AccessBreaker support: clears mech locks on EmagType.Access, same as doors.
    /// </summary>
    private void OnEmagged(EntityUid uid, MechLockComponent component, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Access))
            return;

        var anyLockedOrRegistered = component.IsLocked || component.DnaLockRegistered || component.CardLockRegistered || component.DnaLockActive || component.CardLockActive;
        if (!anyLockedOrRegistered)
            return;

        // Reset both lock types completely
        component.DnaLockRegistered = false;
        component.DnaLockActive = false;
        component.OwnerDna = null;

        component.CardLockRegistered = false;
        component.CardLockActive = false;
        component.OwnerJobTitle = null;
        component.CardAccessTags = null;

        UpdateLockState(uid, component);
        UpdateMechUI(uid);

        args.Handled = true;
        args.Repeatable = true;
    }
}
