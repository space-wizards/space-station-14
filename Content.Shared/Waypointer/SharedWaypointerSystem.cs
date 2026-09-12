using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Waypointer.Components;
using Content.Shared.Waypointer.Events;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared.Waypointer;

/// <summary>
/// This solely handles giving the Waypoint component to equipees. This cannot be done on client, or else it would.
/// </summary>
public abstract partial class SharedWaypointerSystem : EntitySystem
{
    [Dependency] protected IGameTiming Timing = default!;
    [Dependency] private SharedActionsSystem _actions = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

    [SubscribeLocalEvent]
    protected virtual void OnMapInit(Entity<ActiveWaypointerComponent> player, ref MapInitEvent args)
    {
        _actions.AddAction(player, ref player.Comp.ActionEntity, player.Comp.ActionProtoId);
    }

    [SubscribeLocalEvent]
    protected virtual void OnShutdown(Entity<ActiveWaypointerComponent> player, ref ComponentShutdown args)
    {
        _actions.RemoveAction(player.Owner, player.Comp.ActionEntity);
    }

    [SubscribeLocalEvent]
    private void OnMapInit(Entity<InnateWaypointerComponent> player, ref MapInitEvent args)
    {
        SetWaypointerComponent(player);
    }

    [SubscribeLocalEvent]
    private void OnActionPressed(Entity<ActiveWaypointerComponent> player, ref ActionManageWaypointersEvent args)
    {
        if (args.Handled)
            return;
        // To avoid adding UserInterfaceComponent on the BaseMob, we open the interface on the action entity, not the player.
        _ui.OpenUi(args.Action.Owner, WaypointerUiKey.Key, player.Owner);
        args.Handled = true;
    }

    [SubscribeLocalEvent]
    protected virtual void OnWaypointersToggled(Entity<ActionComponent> action, ref WaypointersToggledMessage args)
    {
        // Messages are sent to the action entity - So we need to get the player from the component.
        if (!TryComp<ActiveWaypointerComponent>(action.Comp.Container, out var waypointer)
            || waypointer.WaypointerProtoIds == null)
            return;

        waypointer.Active = args.IsActive;
        _actions.SetToggled(action.AsNullable(), args.IsActive);

        Dirty(action.Comp.Container.Value, waypointer);
    }

    [SubscribeLocalEvent]
    private void OnWaypointersStatusChanged(Entity<ActionComponent> action, ref WaypointerStatusChangedMessage args)
    {
        // Messages are sent to the action entity - So we need to get the player from the component.
        if (!TryComp<ActiveWaypointerComponent>(action.Comp.Container, out var waypointer)
            || waypointer.WaypointerProtoIds == null)
            return;

        waypointer.WaypointerProtoIds[args.ToggledWaypointerProtoId] = !waypointer.WaypointerProtoIds[args.ToggledWaypointerProtoId];

        Dirty(action.Comp.Container.Value, waypointer);
    }

    [SubscribeLocalEvent]
    private void OnEquip(Entity<ClothingShowWaypointerComponent> clothing, ref ClothingGotEquippedEvent args)
    {
        SetWaypointerComponent(args.Wearer);
    }

    [SubscribeLocalEvent]
    private void OnUnequip(Entity<ClothingShowWaypointerComponent> clothing, ref ClothingGotUnequippedEvent args)
    {
        SetWaypointerComponent(args.Wearer);
    }

    [SubscribeLocalEvent]
    private void OnWaypointerChanged(Entity<InnateWaypointerComponent> clothing, ref WaypointerChangedEvent args)
    {
        args.Waypointers.UnionWith(clothing.Comp.WaypointerProtoIds);
    }

    [SubscribeLocalEvent]
    private void OnWaypointerChanged(Entity<ClothingShowWaypointerComponent> clothing, ref InventoryRelayedEvent<WaypointerChangedEvent> args)
    {
        args.Args.Waypointers.UnionWith(clothing.Comp.WaypointerProtoIds);
    }

    private void SetWaypointerComponent(EntityUid player)
    {
        if (Timing.ApplyingState)
             return;

        // We raise this on the entity to check for anything that could give the entity a waypointer.
        var ev = new WaypointerChangedEvent();
        RaiseLocalEvent(player, ref ev);

        if (ev.Waypointers.Count == 0)
        {
            RemCompDeferred<ActiveWaypointerComponent>(player);
            return;
        }

        var comp = EnsureComp<ActiveWaypointerComponent>(player);

        foreach (var waypointers in comp.WaypointerProtoIds ??= [])
        {
            // Remove any lost waypointers.
            if (!ev.Waypointers.Contains(waypointers.Key))
                comp.WaypointerProtoIds.Remove(waypointers.Key);
            // If they still have them, we don't need to add them later.
            else
                ev.Waypointers.Remove(waypointers.Key);
        }
        // Now there should be only new waypointers left in the hashset.
        foreach (var newWaypointer in ev.Waypointers)
        {
            // Little sanity check for when it somehow tries to add an existing waypointer.
            DebugTools.Assert(comp.WaypointerProtoIds.TryAdd(newWaypointer, true));
        }

        Dirty(player, comp);
    }
}

[Serializable, NetSerializable]
public enum WaypointerUiKey : byte
{
    Key,
}
