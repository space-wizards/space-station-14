using Robust.Shared.Containers;

namespace Content.Shared.Containers.ItemSlots;

/// <summary>
/// Handles <see cref="ItemSlotsFillComponent"/> spawning on MapInit.
/// </summary>
public sealed class ItemSlotsFillSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly ItemSlotsSystem _slots = default!;

    private EntityQuery<ItemSlotsComponent> _slotsQuery;

    public override void Initialize()
    {
        base.Initialize();

        _slotsQuery = GetEntityQuery<ItemSlotsComponent>();

        SubscribeLocalEvent<ItemSlotsFillComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ItemSlotsFillComponent> ent, ref MapInitEvent args)
    {
        if (!_slotsQuery.TryComp(ent, out var slots))
        {
            Log.Error("ItemSlotsFill used on entity that has no ItemSlots!");
            return;
        }

        var coords = Transform(ent).Coordinates;
        foreach (var (slotId, item) in ent.Comp.Items)
        {
            if (!_slots.TryGetSlot(ent, slotId, out var slot, slots))
            {
                Log.Error($"Tried to fill item {item} into non-existent slot {slotId}!");
                continue;
            }

            var uid = Spawn(item, coords);
            if (slot.ContainerSlot == null)
            {
                Log.Error($"Tried to fill item {ToPrettyString(uid):item} into slot {slotId} that had no container! It has been dropped on it.");
                continue;
            }

            _container.Insert(uid, slot.ContainerSlot);
        }
    }
}
