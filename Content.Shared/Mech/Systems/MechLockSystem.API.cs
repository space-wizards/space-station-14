using Content.Shared.Access.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Access;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using JetBrains.Annotations;

namespace Content.Shared.Mech.Systems;

public sealed partial class MechLockSystem
{
    /// <summary>
    /// Updates the overall lock state based on individual lock states.
    /// </summary>
    [PublicAPI]
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
    /// Checks if the user has access to the mech.
    /// </summary>
    [PublicAPI]
    public bool CheckAccess(EntityUid uid, EntityUid user, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        var hasAccess = HasAccess(user, component);
        return hasAccess;
    }

    /// <summary>
    /// Checks if the user has access to the mech and provides feedback if denied.
    /// </summary>
    [PublicAPI]
    public bool CheckAccessWithFeedback(EntityUid uid, EntityUid user, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return true;

        if (HasAccess(user, component))
        {
            return true;
        }

        // Access denied - show popup and play sound.
        _popup.PopupPredicted(Loc.GetString("mech-lock-access-denied-popup"), uid, user);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg"), uid, AudioParams.Default.WithVolume(-5f));
        return false;
    }

    /// <summary>
    /// Attempts to register a lock for the specified user.
    /// </summary>
    [PublicAPI]
    public bool TryRegisterLock(EntityUid uid, EntityUid user, MechLockType lockType, MechLockComponent? component = null)
    {
        if (!Resolve(uid, ref component))
            return false;

        switch (lockType)
        {
            case MechLockType.Dna:
                if (!TryComp<DnaComponent>(user, out var dnaComp))
                {
                    _popup.PopupPredicted(Loc.GetString("mech-lock-no-dna-popup"), uid, user);
                    return false;
                }
                component.DnaLockRegistered = true;
                component.OwnerDna = dnaComp.DNA;
                _popup.PopupPredicted(Loc.GetString("mech-lock-dna-registered-popup"), uid, user);
                break;

            case MechLockType.Card:
                if (!TryFindIdCard(user, out var idCard))
                {
                    _popup.PopupPredicted(Loc.GetString("mech-lock-no-card-popup"), uid, user);
                    return false;
                }
                component.CardLockRegistered = true;
                component.OwnerJobTitle = idCard.Comp.LocalizedJobTitle;
                if (TryComp<AccessComponent>(idCard.Owner, out var access) && access != null && access.Tags != null)
                {
                    component.CardAccessTags = new HashSet<ProtoId<AccessLevelPrototype>>(access.Tags);
                }
                _popup.PopupPredicted(Loc.GetString("mech-lock-card-registered-popup"), uid, user);
                break;
        }

        UpdateLockState(uid, component);
        UpdateMechUI(uid);
        return true;
    }

    /// <summary>
    /// Toggles lock state.
    /// </summary>
    [PublicAPI]
    public bool TryToggleLock(EntityUid uid, MechLockType lockType, MechLockComponent? component = null)
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
    /// Resets lock system.
    /// </summary>
    [PublicAPI]
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
        _popup.PopupPredicted(Loc.GetString("mech-lock-reset-success-popup"), uid, user);
        return true;
    }

    /// <summary>
    /// Gets lock state for a specific lock type.
    /// </summary>
    [PublicAPI]
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
    /// Shows appropriate lock state message to user.
    /// </summary>
    [PublicAPI]
    public void ShowLockMessage(EntityUid uid, EntityUid user, bool isActivating)
    {
        var messageKey = isActivating ? "mech-lock-activated-popup" : "mech-lock-deactivated-popup";
        _popup.PopupPredicted(Loc.GetString(messageKey), uid, user);
    }
}

/// <summary>
/// Event raised when the mech lock state changes.
/// </summary>
public sealed class MechLockStateChangedEvent(bool isLocked) : EntityEventArgs
{
    public bool IsLocked { get; } = isLocked;
}
