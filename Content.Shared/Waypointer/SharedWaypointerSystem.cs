using System.Linq;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Waypointer.Components;
using Content.Shared.Waypointer.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;

namespace Content.Shared.Waypointer;

/// <summary>
/// This solely handles giving the Waypoint component to equipees. This cannot be done on client, or else it would.
/// </summary>
public abstract partial class SharedWaypointerSystem : EntitySystem
{
    [Dependency] protected SharedActionsSystem Actions = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedUserInterfaceSystem _ui = default!;

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
        Actions.SetToggled(action.AsNullable(), args.IsActive);

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
        if (_timing.ApplyingState)
             return;

        var comp = EnsureComp<ActiveWaypointerComponent>(player);
        // The following is much easier to do with HashSets.
        HashSet<ProtoId<WaypointerPrototype>>? previousTable = null;
        HashSet<ProtoId<WaypointerPrototype>>? overridesToRemove = null;
        if (comp.WaypointerProtoIds != null)
        {
            // Store the hashset for comparison later, if not null
            previousTable = comp.WaypointerProtoIds.Keys.ToHashSet();
            // The same as above. As an example, these have the values A and B. { A, B }
            overridesToRemove = comp.WaypointerProtoIds.Keys.ToHashSet();
        }

        // We raise this on the entity to check for anything that could give the entity a waypointer.
        var ev = new WaypointerChangedEvent();
        RaiseLocalEvent(player, ref ev); // For example purposes, this will return { B, C }

        if (overridesToRemove != null)
        {
            // Now we remove all the waypointers that the event gathered from the previous hashset.
            // Removing { B, C } will leave us with { A }, as we don't have the waypointer A anymore.
            overridesToRemove.ExceptWith(ev.Waypointers);
            // We remove the overrides for the waypointer A.
            RemoveOverrides(player, overridesToRemove);
        }

        if (ev.Waypointers.Count == 0)
        {
            RemComp<ActiveWaypointerComponent>(player);
            return;
        }

        // We need to turn it into a dictionary so we can keep track of every waypointer status, not important for overrides.
        var newDict = ev.Waypointers.ToDictionary(key => key, _ => true);
        if (comp.WaypointerProtoIds != null)
        {
            foreach (var pair in comp.WaypointerProtoIds.Where(pair => newDict.ContainsKey(pair.Key)))
            {
                // We set the status of every waypointer that persisted to their original value, not important for overrides.
                newDict[pair.Key] = pair.Value;
            }
        }
        // Then replace the old dictionary with the new one
        comp.WaypointerProtoIds = newDict;

        // Here we now remove every waypointer that doesn't need new overrides.
        // The waypointer B was already overriden, since it's in the earlier hashset { A, B }
        // So, by removing { A, b} from { B, C }, we get { C }, which is the only new waypointer - thus needing overrides.
        if (previousTable != null)
            ev.Waypointers.ExceptWith(previousTable);

        AddOverrides(player, ev.Waypointers);
        Dirty(player, comp);
    }

    protected virtual void AddOverrides(EntityUid player, HashSet<ProtoId<WaypointerPrototype>> waypointers)
    {
    }

    protected virtual void RemoveOverrides(EntityUid player, HashSet<ProtoId<WaypointerPrototype>> waypointers)
    {
    }
}

[Serializable, NetSerializable]
public enum WaypointerUiKey : byte
{
    Key,
}
