using Content.Shared.Clothing;
using Content.Shared.Inventory;
using Content.Shared.Waypointer.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Shared.Waypointer;

/// <summary>
/// This solely handles giving the Waypoint component to equipees. This cannot be done on client, or else it would.
/// </summary>
public abstract class SharedWaypointerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<InnateWaypointerComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<WaypointerComponent, ActionToggleWaypointersEvent>(OnActionToggle);

        SubscribeLocalEvent<ClothingShowWaypointerComponent, ClothingGotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<ClothingShowWaypointerComponent, ClothingGotUnequippedEvent>(OnUnequip);

        SubscribeLocalEvent<InnateWaypointerComponent, WaypointerChangedEvent>(OnWaypointerChanged);
        SubscribeLocalEvent<ClothingShowWaypointerComponent, InventoryRelayedEvent<WaypointerChangedEvent>>(OnWaypointerChanged);
    }

    private void OnMapInit(Entity<InnateWaypointerComponent> player, ref MapInitEvent args)
    {
        SetWaypointerComponent(player);
    }

    protected virtual void OnActionToggle(Entity<WaypointerComponent> mob, ref ActionToggleWaypointersEvent args)
    {
        if (args.Handled)
            return;

        // Without this in Shared, the action doesn't toggle.
        args.Toggle = true;
        args.Handled = true;
    }

    private void OnEquip(Entity<ClothingShowWaypointerComponent> clothing, ref ClothingGotEquippedEvent args)
    {
        SetWaypointerComponent(args.Wearer);
    }

    private void OnUnequip(Entity<ClothingShowWaypointerComponent> clothing, ref ClothingGotUnequippedEvent args)
    {
        SetWaypointerComponent(args.Wearer);
    }

    private void OnWaypointerChanged(Entity<InnateWaypointerComponent> clothing, ref WaypointerChangedEvent args)
    {
        args.Waypointers.UnionWith(clothing.Comp.WaypointerProtoIds);
    }

    private void OnWaypointerChanged(Entity<ClothingShowWaypointerComponent> clothing, ref InventoryRelayedEvent<WaypointerChangedEvent> args)
    {
        args.Args.Waypointers.UnionWith(clothing.Comp.WaypointerProtoIds);
    }

    private void SetWaypointerComponent(EntityUid player)
    {
        if (_timing.ApplyingState)
             return;

        var comp = EnsureComp<WaypointerComponent>(player);
        // Store the hashset for comparison later.
        var previousTable = comp.WaypointerProtoIds;
        // The same as above. As an example, these have the values A and B. { A, B }
        var overridesToRemove = comp.WaypointerProtoIds;

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

        if (ev.Waypointers.Count == 0) // Self-Explanatory - If there are no waypointers left, remove the component.
        {
            RemComp<WaypointerComponent>(player);
            return;
        }

        comp.WaypointerProtoIds = ev.Waypointers;

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
