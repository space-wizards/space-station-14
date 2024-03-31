using Content.Server.Lock.Components;
using Content.Server.Popups;
using Content.Shared.UserInterface;
using Content.Shared.Lock;
using Content.Server.UserInterface;

namespace Content.Server.Lock.EntitySystems;
public sealed class ActivatableUIRequiresLockSystem : EntitySystem
{
    [Dependency] private readonly ActivatableUISystem _activatableUI = default!;
    [Dependency] private readonly PopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActivatableUIRequiresLockComponent, ActivatableUIOpenAttemptEvent>(OnUIOpenAttempt);
        SubscribeLocalEvent<ActivatableUIRequiresLockComponent, LockToggledEvent>(LockToggled);
    }

    private void OnUIOpenAttempt(EntityUid uid, ActivatableUIRequiresLockComponent component, ActivatableUIOpenAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (TryComp<LockComponent>(uid, out var lockComp) && lockComp.Locked != component.requireLocked)
        {
            args.Cancel();
            if (lockComp.Locked)
                _popupSystem.PopupEntity(Loc.GetString("entity-storage-component-locked-message"), uid, args.User);
        }
    }

    private void LockToggled(EntityUid uid, ActivatableUIRequiresLockComponent component, LockToggledEvent args)
    {
        if (!TryComp<LockComponent>(uid, out var lockComp) || lockComp.Locked == component.requireLocked)
            return;

        _activatableUI.CloseAll(uid);
    }
}

