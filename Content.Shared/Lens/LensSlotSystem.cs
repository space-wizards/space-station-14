using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Shared.Lens;

public sealed class LensSlotSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LensSlotComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<LensSlotComponent, EntInsertedIntoContainerMessage>(OnLensInserted);
        SubscribeLocalEvent<LensSlotComponent, EntRemovedFromContainerMessage>(OnLensRemoved);
    }

    private void OnExamine(EntityUid glasses, LensSlotComponent component, ref ExaminedEvent args)
    {
        if (!_itemSlots.TryGetSlot(glasses, component.LensSlotId, out var itemSlot))
            return;

        var msg = new FormattedMessage();

        if (itemSlot.Item == null)
            msg.AddMarkupOrThrow(Loc.GetString("lens-empty"));
        else
        {
            var metadata = MetaData(itemSlot.Item.Value);
            msg.AddMarkupOrThrow(Loc.GetString("lens-filled") + " [color=white]" + metadata.EntityName + "[/color].");
        }

        args.PushMessage(msg);
    }

        private void OnLensInserted(EntityUid glasses, LensSlotComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.LensSlotId)
            return;

        RaiseLocalEvent(glasses, new LensChangedEvent(false));
    }

        private void OnLensRemoved(EntityUid glasses, LensSlotComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.LensSlotId)
            return;

        RaiseLocalEvent(glasses, new LensChangedEvent(true));
    }
}
