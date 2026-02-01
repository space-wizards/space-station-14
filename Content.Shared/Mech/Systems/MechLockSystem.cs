using System.Linq;
using Content.Shared.Access;
using Content.Shared.Access.Components;
using Content.Shared.Access.Systems;
using Content.Shared.Emag.Systems;
using Content.Shared.Forensics.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Popups;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Prototypes;

namespace Content.Shared.Mech.Systems;

/// <summary>
/// System responsible for handling mech locking logic.
/// </summary>
public sealed partial class MechLockSystem : EntitySystem
{
    [Dependency] private readonly EmagSystem _emag = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedIdCardSystem _idCard = default!;
    [Dependency] private readonly SharedMechSystem _mech = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

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

    private void OnDnaLockRegister(Entity<MechLockComponent> ent, ref MechDnaLockRegisterEvent args)
    {
        RegisterLock(ent, GetEntity(args.User), MechLockType.Dna);
    }

    private void OnDnaLockToggle(Entity<MechLockComponent> ent, ref MechDnaLockToggleEvent args)
    {
        TryToggleLock(ent, MechLockType.Dna);
    }

    private void OnDnaLockReset(Entity<MechLockComponent> ent, ref MechDnaLockResetEvent args)
    {
        ResetLock(ent, MechLockType.Dna);
    }
    private void OnCardLockRegister(Entity<MechLockComponent> ent, ref MechCardLockRegisterEvent args)
    {
        RegisterLock(ent, GetEntity(args.User), MechLockType.Card);
    }

    private void OnCardLockToggle(Entity<MechLockComponent> ent, ref MechCardLockToggleEvent args)
    {
        TryToggleLock(ent, MechLockType.Card);
    }

    private void OnCardLockReset(Entity<MechLockComponent> ent, ref MechCardLockResetEvent args)
    {
        ResetLock(ent, MechLockType.Card);
    }

    /// <summary>
    /// Checks if a user has access to a locked mech and can manage locks
    /// </summary>
    private bool HasAccess(EntityUid user, Entity<MechLockComponent> ent)
    {
        // If no locks are registered or active, access is granted
        if (ent.Comp is { DnaLockRegistered: false, CardLockRegistered: false } or { DnaLockActive: false, CardLockActive: false })
            return true;

        // If any locks are registered, user must be registered for at least one type
        var isRegisteredForAny = IsAnyOwner(user, ent);
        if (!isRegisteredForAny)
            return false;

        // Check DNA lock - if active, user must have matching DNA
        if (ent.Comp is { DnaLockActive: true, DnaLockRegistered: true })
        {
            if (TryComp<DnaComponent>(user, out var dnaComp)
                && dnaComp.DNA == ent.Comp.OwnerDna)
                return true;
        }

        // Check card lock - if active, user must have matching access tags
        if (ent.Comp is { CardLockActive: true, CardLockRegistered: true })
        {
            if (!_idCard.TryFindIdCard(user, out var idCard)
                || !TryComp<AccessComponent>(idCard.Owner, out var access))
                return false;

            var tags = access.Tags;
            foreach (var tag in tags)
            {
                if (ent.Comp.CardAccessTags!.Contains(tag))
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Resets a lock for the specified user.
    /// </summary>
    private void ResetLock(Entity<MechLockComponent> ent, MechLockType lockType)
    {
        switch (lockType)
        {
            case MechLockType.Dna:
                ent.Comp.DnaLockRegistered = false;
                ent.Comp.DnaLockActive = false;
                ent.Comp.OwnerDna = null;
                break;

            case MechLockType.Card:
                ent.Comp.CardLockRegistered = false;
                ent.Comp.CardLockActive = false;
                ent.Comp.OwnerJobTitle = null;
                ent.Comp.CardAccessTags = null;
                break;

            default:
                return;
        }

        UpdateLockState(ent.AsNullable());
    }

    /// <summary>
    /// Registers a lock for the specified user.
    /// </summary>
    private void RegisterLock(Entity<MechLockComponent> ent,
        EntityUid user,
        MechLockType lockType)
    {
        switch (lockType)
        {
            case MechLockType.Dna:
                if (!TryComp<DnaComponent>(user, out var dnaComp))
                {
                    _popup.PopupCursor(Loc.GetString("mech-lock-no-dna-popup"), user);
                    return;
                }

                ent.Comp.DnaLockRegistered = true;
                ent.Comp.OwnerDna = dnaComp.DNA;
                _popup.PopupCursor(Loc.GetString("mech-lock-dna-registered-popup"), user);
                break;

            case MechLockType.Card:
                if (!_idCard.TryFindIdCard(user, out var idCard))
                {
                    _popup.PopupCursor(Loc.GetString("mech-lock-no-card-popup"), user);
                    return;
                }

                ent.Comp.CardLockRegistered = true;
                ent.Comp.OwnerJobTitle = idCard.Comp.LocalizedJobTitle;
                if (TryComp<AccessComponent>(idCard.Owner, out var access))
                    ent.Comp.CardAccessTags = new HashSet<ProtoId<AccessLevelPrototype>>(access.Tags);

                _popup.PopupCursor(Loc.GetString("mech-lock-card-registered-popup"), user);
                break;

            default:
                return;
        }

        UpdateLockState(ent.AsNullable());
    }

    /// <summary>
    /// Toggles lock state.
    /// </summary>
    private void TryToggleLock(Entity<MechLockComponent> ent, MechLockType lockType)
    {
        switch (lockType)
        {
            case MechLockType.Dna:
                if (!ent.Comp.DnaLockRegistered)
                    return;
                ent.Comp.DnaLockActive = !ent.Comp.DnaLockActive;
                break;

            case MechLockType.Card:
                if (!ent.Comp.CardLockRegistered)
                    return;
                ent.Comp.CardLockActive = !ent.Comp.CardLockActive;
                break;

            default:
                return;
        }

        UpdateLockState(ent.AsNullable());
    }

    /// <summary>
    /// Checks if the user is the owner of the lock.
    /// </summary>
    private bool IsOwnerOfLock(EntityUid user, Entity<MechLockComponent> ent, MechLockType lockType)
    {
        var (isRegistered, _, ownerId) = GetLockState(lockType, ent.Comp);
        if (!isRegistered || ownerId == null)
            return false;

        switch (lockType)
        {
            case MechLockType.Dna:
                return TryComp<DnaComponent>(user, out var dnaComp) && dnaComp.DNA == ownerId;

            case MechLockType.Card:
                if (ent.Comp.CardAccessTags == null || ent.Comp.CardAccessTags.Count == 0)
                    return false;
                if (!_idCard.TryFindIdCard(user, out var idCard))
                    return false;
                if (!TryComp<AccessComponent>(idCard.Owner, out var access))
                    return false;

                var tags = access.Tags;

                return ent.Comp.CardAccessTags.Any(tags.Contains);

            default:
                return false;
        }
    }

    private bool IsAnyOwner(EntityUid user, Entity<MechLockComponent> ent)
    {
        return IsOwnerOfLock(user, ent, MechLockType.Dna) || IsOwnerOfLock(user, ent, MechLockType.Card);
    }

    private void OnLockStartup(Entity<MechLockComponent> ent, ref ComponentStartup args)
    {
        UpdateLockState(ent.AsNullable());
    }

    /// <summary>
    /// AccessBreaker: clears mech locks on EmagType.Access, same as doors.
    /// </summary>
    private void OnEmagged(Entity<MechLockComponent> ent, ref GotEmaggedEvent args)
    {
        if (!_emag.CompareFlag(args.Type, EmagType.Access))
            return;

        var anyLockedOrRegistered = ent.Comp.IsLocked || ent.Comp.DnaLockRegistered || ent.Comp.CardLockRegistered ||
                                    ent.Comp.DnaLockActive || ent.Comp.CardLockActive;
        if (!anyLockedOrRegistered)
            return;

        // Reset both lock types completely.
        ent.Comp.DnaLockRegistered = false;
        ent.Comp.DnaLockActive = false;
        ent.Comp.OwnerDna = null;

        ent.Comp.CardLockRegistered = false;
        ent.Comp.CardLockActive = false;
        ent.Comp.OwnerJobTitle = null;
        ent.Comp.CardAccessTags = null;

        UpdateLockState(ent.AsNullable());

        args.Handled = true;
        args.Repeatable = true;
    }
}
