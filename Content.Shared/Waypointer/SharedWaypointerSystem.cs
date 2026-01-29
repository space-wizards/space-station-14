using Content.Shared.Inventory.Events;

namespace Content.Shared.Waypointer;

/// <summary>
/// This solely handles giving the Waypoint component to equipees. This cannot be done on client, or else it would.
/// </summary>
public abstract class SharedWaypointerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<WaypointerComponent, ActionToggleWaypointersEvent>(OnActionToggle);

        SubscribeLocalEvent<ShowWaypointerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<ShowWaypointerComponent, GotUnequippedEvent>(OnUnequip);
    }

    protected virtual void OnActionToggle(Entity<WaypointerComponent> mob, ref ActionToggleWaypointersEvent args)
    {
        if (args.Handled)
            return;

        // Without this in Shared, the action doesn't toggle.
        args.Toggle = true;
        args.Handled = true;
    }

    private void OnEquip(Entity<ShowWaypointerComponent> clothing, ref GotEquippedEvent args)
    {
        if (HasComp<WaypointerComponent>(args.Equipee))
            return;

        var comp = new WaypointerComponent
        {
            // We're doing it this way, so ComponentInitEvent doesn't fire without this set.
            WaypointerProtoIds = clothing.Comp.WaypointerProtoIds,
        };

        AddComp(args.Equipee, comp);
        Dirty(args.Equipee, comp);
    }

    private void OnUnequip(Entity<ShowWaypointerComponent> clothing, ref GotUnequippedEvent args)
    {
        RemComp<WaypointerComponent>(args.Equipee);
    }
}
