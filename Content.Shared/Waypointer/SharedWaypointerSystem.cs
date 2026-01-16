using Content.Shared.Inventory.Events;

namespace Content.Shared.Waypointer;

/// <summary>
/// This solely handles giving the Waypoint component to equipees. This cannot be done on client, or else it would.
/// </summary>
public abstract class SharedWaypointerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<ShowWaypointerComponent, GotEquippedEvent>(OnEquip);
        SubscribeLocalEvent<ShowWaypointerComponent, GotUnequippedEvent>(OnUnequip);
    }

    private void OnEquip(Entity<ShowWaypointerComponent> clothing, ref GotEquippedEvent args)
    {
        var comp = EnsureComp<WaypointerComponent>(args.Equipee);
        comp.WaypointerProtoId = clothing.Comp.WaypointerProtoId;
        Dirty(args.Equipee, comp);
    }

    private void OnUnequip(Entity<ShowWaypointerComponent> clothing, ref GotUnequippedEvent args)
    {
        RemComp<WaypointerComponent>(args.Equipee);
    }
}
