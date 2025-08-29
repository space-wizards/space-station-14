using Content.Server.Access.Systems;
using Content.Shared.Access.Components;
using Content.Shared.Mech.Components;
using Content.Shared.Mech.EntitySystems;
using Content.Shared.Popups;

namespace Content.Server.Mech.Systems;

/// <summary>
/// Server-side system for mech lock functionality
/// </summary>
public sealed class MechLockSystem : SharedMechLockSystem
{
    [Dependency] private readonly IdCardSystem _idCard = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MechLockComponent, MechDnaLockRegisterEvent>(OnDnaLockRegister);
        SubscribeLocalEvent<MechLockComponent, MechDnaLockToggleEvent>(OnDnaLockToggle);
        SubscribeLocalEvent<MechLockComponent, MechDnaLockResetEvent>(OnDnaLockReset);
        SubscribeLocalEvent<MechLockComponent, MechCardLockRegisterEvent>(OnCardLockRegister);
        SubscribeLocalEvent<MechLockComponent, MechCardLockToggleEvent>(OnCardLockToggle);
        SubscribeLocalEvent<MechLockComponent, MechCardLockResetEvent>(OnCardLockReset);
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

        if (TryToggleLock(uid, user, MechLockType.Dna, component))
        {
            var (_, isActive, _) = GetLockState(MechLockType.Dna, component);
            ShowLockMessage(uid, user, component, isActive);
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

        if (TryToggleLock(uid, user, MechLockType.Card, component))
        {
            var (_, isActive, _) = GetLockState(MechLockType.Card, component);
            ShowLockMessage(uid, user, component, isActive);
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

    protected override void UpdateMechUI(EntityUid uid)
    {
        var ev = new UpdateMechUiEvent();
        RaiseLocalEvent(uid, ev);
    }

    protected override bool TryFindIdCard(EntityUid user, out Entity<IdCardComponent> idCard)
    {
        return _idCard.TryFindIdCard(user, out idCard);
    }
}
