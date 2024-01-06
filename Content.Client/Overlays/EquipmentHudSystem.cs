using Content.Shared.GameTicking;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Client.Player;
using Robust.Shared.Player;

namespace Content.Client.Overlays;

/// <summary>
/// This is a base system to make it easier to enable or disabling UI elements based on whether or not the player has
/// some component, either on their controlled entity on some worn piece of equipment.
/// </summary>
public abstract class EquipmentHudSystem<T> : EntitySystem where T : IComponent
{
    [Dependency] private readonly IPlayerManager _player = default!;

    protected bool IsActive;
    protected virtual SlotFlags TargetSlots => ~SlotFlags.POCKET;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<T, ComponentStartup>(OnStartup);
        SubscribeLocalEvent<T, ComponentRemove>(OnRemove);

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<T, GotEquippedEvent>(OnCompEquip);
        SubscribeLocalEvent<T, GotUnequippedEvent>(OnCompUnequip);

        SubscribeLocalEvent<T, RefreshEquipmentHudEvent<T>>(OnRefreshComponentHud);
        SubscribeLocalEvent<T, InventoryRelayedEvent<RefreshEquipmentHudEvent<T>>>(OnRefreshEquipmentHud);

        SubscribeLocalEvent<RoundRestartCleanupEvent>(OnRoundRestart);
    }

    private void Update(RefreshEquipmentHudEvent<T> ev)
    {
        IsActive = true;
        UpdateInternal(ev);
    }

    public void Deactivate()
    {
        if (!IsActive)
            return;

        IsActive = false;
        DeactivateInternal();
    }

    protected virtual void UpdateInternal(RefreshEquipmentHudEvent<T> args) { }

    protected virtual void DeactivateInternal() { }

    private void OnStartup(EntityUid uid, T component, ComponentStartup args)
    {
        RefreshOverlay(uid);
    }

    private void OnRemove(EntityUid uid, T component, ComponentRemove args)
    {
        RefreshOverlay(uid);
    }

    private void OnPlayerAttached(LocalPlayerAttachedEvent args)
    {
        RefreshOverlay(args.Entity);
    }

    private void OnPlayerDetached(LocalPlayerDetachedEvent args)
    {
        if (_player.LocalSession?.AttachedEntity == null)
            Deactivate();
    }

    private void OnCompEquip(EntityUid uid, T component, GotEquippedEvent args)
    {
        RefreshOverlay(args.Equipee);
    }

    private void OnCompUnequip(EntityUid uid, T component, GotUnequippedEvent args)
    {
        RefreshOverlay(args.Equipee);
    }

    private void OnRoundRestart(RoundRestartCleanupEvent args)
    {
        Deactivate();
    }

    protected virtual void OnRefreshEquipmentHud(EntityUid uid, T component, InventoryRelayedEvent<RefreshEquipmentHudEvent<T>> args)
    {
        OnRefreshComponentHud(uid, component, args.Args);
    }

    protected virtual void OnRefreshComponentHud(EntityUid uid, T component, RefreshEquipmentHudEvent<T> args)
    {
        args.Active = true;
        args.Components.Add(component);
    }

    private void RefreshOverlay(EntityUid uid)
    {
        if (uid != _player.LocalSession?.AttachedEntity)
            return;

        var ev = new RefreshEquipmentHudEvent<T>(TargetSlots);
        RaiseLocalEvent(uid, ev);

        if (ev.Active)
            Update(ev);
        else
            Deactivate();
    }
}
