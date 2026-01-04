using JetBrains.Annotations;
using Content.Shared.Mech.Components;
using Robust.Shared.Audio;

namespace Content.Shared.Mech.Systems;

public sealed partial class MechLockSystem
{
    /// <summary>
    /// Updates the overall lock state based on individual lock states.
    /// </summary>
    [PublicAPI]
    public void UpdateLockState(Entity<MechLockComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var wasLocked = ent.Comp.IsLocked;
        ent.Comp.IsLocked = ent.Comp.DnaLockActive || ent.Comp.CardLockActive;
        Dirty(ent);
        _mech.UpdateMechUi(ent.Owner);

        if (wasLocked != ent.Comp.IsLocked)
        {
            var lockEvent = new MechLockStateChangedEvent(ent.Comp.IsLocked);
            RaiseLocalEvent(ent.Owner, lockEvent);
        }
    }

    /// <summary>
    /// Checks if the user has access to the mech.
    /// </summary>
    [PublicAPI]
    public bool CheckAccess(Entity<MechLockComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return true;

        return HasAccess(user, (ent.Owner, ent.Comp));
    }

    /// <summary>
    /// Checks if the user has access to the mech and provides feedback if denied.
    /// </summary>
    [PublicAPI]
    public bool CheckAccessWithFeedback(Entity<MechLockComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp))
            return true;

        if (HasAccess(user, (ent.Owner, ent.Comp)))
            return true;

        // Access denied - show popup and play sound.
        _popup.PopupCursor(Loc.GetString("mech-lock-access-denied-popup"), user);
        _audio.PlayPvs(new SoundPathSpecifier("/Audio/Machines/custom_deny.ogg"), ent.Owner);

        return false;
    }

    /// <summary>
    /// Gets lock state for a specific lock type.
    /// </summary>
    [PublicAPI]
    public (bool IsRegistered, bool IsActive, string? OwnerId) GetLockState(MechLockType lockType,
        MechLockComponent component)
    {
        return lockType switch
        {
            MechLockType.Dna => (component.DnaLockRegistered, component.DnaLockActive, component.OwnerDna),
            MechLockType.Card => (component.CardLockRegistered, component.CardLockActive, component.OwnerJobTitle),
            _ => (false, false, null)
        };
    }
}

/// <summary>
/// Event raised when the mech lock state changes.
/// </summary>
public sealed class MechLockStateChangedEvent(bool isLocked) : EntityEventArgs
{
    public bool IsLocked { get; } = isLocked;
}
