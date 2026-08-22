using Content.Shared.Actions;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;

namespace Content.Shared.XRayVision;

/// <summary>
/// Shows/hides the <see cref="XRayVisionOverlay"/> based on whether the observed
/// entity has a <see cref="XRayVisionComponent"/> equipped.
/// </summary>
public abstract partial class SharedXRayVisionSystem : EntitySystem
{
    [Dependency] private SharedActionsSystem _actions = default!;

    [SubscribeLocalEvent]
    private void OnStartup(Entity<XRayVisionComponent> ent, ref ComponentStartup args)
    {
        if (ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(ent);
        _actions.AddAction(ent, ref ent.Comp.ActionEntity, ent.Comp.Action);
    }

    [SubscribeLocalEvent]
    private void OnRemove(Entity<XRayVisionComponent> ent, ref ComponentRemove args)
    {
        if (ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(ent);
        _actions.RemoveAction(ent.Owner, ent.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnCompEquip(Entity<XRayVisionComponent> ent, ref GotEquippedEvent args)
    {
        if (!ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(args.EquipTarget);
        _actions.AddAction(args.EquipTarget, ref ent.Comp.ActionEntity, ent.Comp.Action, ent);
    }

    [SubscribeLocalEvent]
    private void OnCompUnequip(Entity<XRayVisionComponent> ent, ref GotUnequippedEvent args)
    {
        if (!ent.Comp.RelayOverlay)
            return;

        RefreshOverlay(args.EquipTarget);
    }

    [SubscribeLocalEvent]
    protected virtual void OnRefreshEquipmentHud(Entity<XRayVisionComponent> ent, ref InventoryRelayedEvent<RefreshXRayVisionEvent> args)
    {
        OnRefreshComponentHud(ent, ref args.Args);
    }

    [SubscribeLocalEvent]
    protected virtual void OnRefreshComponentHud(Entity<XRayVisionComponent> ent, ref RefreshXRayVisionEvent args)
    {
        if (!ent.Comp.Enabled)
            return;

        args.Entities.Add(ent);
    }

    [SubscribeLocalEvent]
    private void OnToggleXRayVisionEvent(ToggleXRayVisionEvent args)
    {
        var ent = args.Action.Comp.Container;

        if (!TryComp<XRayVisionComponent>(ent, out var xrayComp))
            return;

        SetEnabled(ent.Value, !xrayComp.Enabled, args.Performer);
        args.Handled = true;
    }

    /// <summary>
    /// Enables or disables the component.
    /// </summary>
    /// <param name="ent">The xray to toggle.</param>
    /// <param name="enabled">Whether to enable or disable.</param>
    /// <param name="viewer">Viewer of the xray, used to refresh their overlay. If null, assumes the xray entity is the viewer.</param>
    public void SetEnabled(Entity<XRayVisionComponent?> ent, bool enabled, EntityUid? viewer = null)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (ent.Comp.Enabled == enabled)
            return;

        ent.Comp.Enabled = enabled;
        Dirty(ent);
        RefreshOverlay(viewer ?? ent);
    }

    protected virtual void RefreshOverlay(EntityUid entity) { }
}

[ByRefEvent]
public record struct RefreshXRayVisionEvent() : IInventoryRelayEvent
{
    public SlotFlags TargetSlots => SlotFlags.WITHOUT_POCKET;
    public List<Entity<XRayVisionComponent>> Entities = new();
}
