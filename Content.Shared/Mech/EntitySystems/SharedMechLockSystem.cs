using Content.Shared.Access.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;
using Content.Shared.Access;
using System.Linq;
using Content.Shared.Emag.Systems;

namespace Content.Shared.Mech.EntitySystems;

/// <summary>
/// System for managing mech lock functionality (DNA and Card locks)
/// </summary>
public abstract partial class SharedMechLockSystem : EntitySystem
{
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly EmagSystem _emag = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechLockComponent, ComponentStartup>(OnLockStartup);
        SubscribeLocalEvent<MechLockComponent, GotEmaggedEvent>(OnEmagged);
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

    /// <summary>
    /// Updates the overall lock state based on individual lock states
    /// </summary>
    public void UpdateLockState(EntityUid uid, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return;

        var wasLocked = component.IsLocked;
        component.IsLocked = component.DnaLockActive || component.CardLockActive;

        if (wasLocked != component.IsLocked)
        {
            Dirty(uid, component);
            var lockEvent = new MechLockStateChangedEvent(component.IsLocked);
            RaiseLocalEvent(uid, lockEvent);
        }
    }

    /// <summary>
    /// Checks if the user has access to the mech
    /// </summary>
    public bool CheckAccess(EntityUid uid, EntityUid user, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        var hasAccess = HasAccess(user, component);
        return hasAccess;
    }

    /// <summary>
    /// Checks if the user has access to the mech and provides feedback if denied
    /// </summary>
    public bool CheckAccessWithFeedback(EntityUid uid, EntityUid user, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        if (HasAccess(user, component))
        {
            return true;
        }

        // Access denied - show popup and play sound
        _popup.PopupEntity(Loc.GetString("mech-lock-access-denied-popup"), uid, user);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg"), uid, AudioParams.Default.WithVolume(-5f));
        return false;
    }

    /// <summary>
    /// Attempts to register a lock for the specified user
    /// </summary>
    public bool TryRegisterLock(EntityUid uid, EntityUid user, MechLockType lockType, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        // Check if the specific lock type is already registered
        var isAlreadyRegistered = lockType switch
        {
            MechLockType.Dna => component.DnaLockRegistered,
            MechLockType.Card => component.CardLockRegistered,
            _ => false
        };

        switch (lockType)
        {
            case MechLockType.Dna:
                if (!TryComp<DnaComponent>(user, out var dnaComp))
                {
                    _popup.PopupEntity(Loc.GetString("mech-lock-no-dna-popup"), uid, user);
                    return false;
                }
                component.DnaLockRegistered = true;
                component.OwnerDna = dnaComp.DNA;
                _popup.PopupEntity(Loc.GetString("mech-lock-dna-registered-popup"), uid, user);
                break;

            case MechLockType.Card:
                if (!TryFindIdCard(user, out var idCard))
                {
                    _popup.PopupEntity(Loc.GetString("mech-lock-no-card-popup"), uid, user);
                    return false;
                }
                component.CardLockRegistered = true;
                component.OwnerJobTitle = idCard.Comp.LocalizedJobTitle;
                if (TryComp<AccessComponent>(idCard.Owner, out var access) && access != null && access.Tags != null)
                {
                    component.CardAccessTags = new HashSet<ProtoId<AccessLevelPrototype>>(access.Tags);
                }
                _popup.PopupEntity(Loc.GetString("mech-lock-card-registered-popup"), uid, user);
                break;
        }

        UpdateLockState(uid, component);
        UpdateMechUI(uid);
        return true;
    }

    /// <summary>
    /// Toggles lock state
    /// </summary>
    public bool TryToggleLock(EntityUid uid, EntityUid user, MechLockType lockType, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        switch (lockType)
        {
            case MechLockType.Dna:
                if (!component.DnaLockRegistered)
                    return false;
                component.DnaLockActive = !component.DnaLockActive;
                break;

            case MechLockType.Card:
                if (!component.CardLockRegistered)
                    return false;
                component.CardLockActive = !component.CardLockActive;
                break;
        }

        UpdateLockState(uid, component);
        UpdateMechUI(uid);
        return true;
    }

    /// <summary>
    /// Resets lock system
    /// </summary>
    public bool TryResetLock(EntityUid uid, EntityUid user, MechLockType lockType, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        switch (lockType)
        {
            case MechLockType.Dna:
                component.DnaLockRegistered = false;
                component.DnaLockActive = false;
                component.OwnerDna = null;
                break;

            case MechLockType.Card:
                component.CardLockRegistered = false;
                component.CardLockActive = false;
                component.OwnerJobTitle = null;
                component.CardAccessTags = null;
                break;
        }

        UpdateLockState(uid, component);
        UpdateMechUI(uid);
        _popup.PopupEntity(Loc.GetString("mech-lock-reset-success-popup"), uid, user);
        return true;
    }

    /// <summary>
    /// Gets lock state for a specific lock type
    /// </summary>
    public (bool IsRegistered, bool IsActive, string? OwnerId) GetLockState(MechLockType lockType, MechLockComponent component)
    {
        return lockType switch
        {
            MechLockType.Dna => (component.DnaLockRegistered, component.DnaLockActive, component.OwnerDna),
            // For card, return the job title as the display string
            MechLockType.Card => (component.CardLockRegistered, component.CardLockActive, component.OwnerJobTitle),
            _ => (false, false, null)
        };
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

    /// <summary>
    /// Shows appropriate lock state message to user
    /// </summary>
    public void ShowLockMessage(EntityUid uid, EntityUid user, MechLockComponent component, bool isActivating)
    {
        var messageKey = isActivating ? "mech-lock-activated-popup" : "mech-lock-deactivated-popup";
        _popup.PopupEntity(Loc.GetString(messageKey), uid, user);
    }

    /// <summary>
    /// Updates mech UI when lock state changes. Override in server systems.
    /// </summary>
    protected virtual void UpdateMechUI(EntityUid uid)
    {
        // Base implementation does nothing - override in server systems
    }

    /// <summary>
    /// Tries to find an ID card. Override in server systems.
    /// </summary>
    protected virtual bool TryFindIdCard(EntityUid user, out Entity<IdCardComponent> idCard)
    {
        idCard = default;
        return false;
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
}

/// <summary>
/// Event raised when the mech lock state changes
/// </summary>
public sealed class MechLockStateChangedEvent(bool isLocked) : EntityEventArgs
{
    public bool IsLocked { get; } = isLocked;
}
