using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.Utility;

namespace Content.Shared.Lens;

/// <summary>
///     Relays <see cref="VisionCorrectionComponent"/> from an item slot.
/// </summary>
public sealed class LensSlotSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly BlurryVisionSystem _Blurry = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LensSlotComponent, ExaminedEvent>(OnExamine);

        SubscribeLocalEvent<LensSlotComponent, GotEquippedEvent>(OnGlassesEquipped);
        SubscribeLocalEvent<LensSlotComponent, GotUnequippedEvent>(OnGlassesUnequipped);
        SubscribeLocalEvent<LensSlotComponent, LensChangedEvent>(OnLensChanged);

        SubscribeLocalEvent<LensSlotComponent, EntInsertedIntoContainerMessage>(OnLensInserted);
        SubscribeLocalEvent<LensSlotComponent, EntRemovedFromContainerMessage>(OnLensRemoved);

        SubscribeLocalEvent<LensSlotComponent, InventoryRelayedEvent<GetBlurEvent>>(OnGetBlurLens);
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

    private void OnGlassesEquipped(Entity<LensSlotComponent> glasses, ref GotEquippedEvent args)
    {
        _Blurry.UpdateBlurMagnitude(args.Equipee);
    }

    private void OnGlassesUnequipped(Entity<LensSlotComponent> glasses, ref GotUnequippedEvent args)
    {
        _Blurry.UpdateBlurMagnitude(args.Equipee);
    }

    private void OnLensChanged(Entity<LensSlotComponent> glasses, ref LensChangedEvent args)
    {
        _Blurry.UpdateBlurMagnitude(Transform(glasses.Owner).ParentUid);
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

    /// <summary>
    ///     Effectively a copy of <see cref="BlurryVisionSystem.OnGetBlur"/>.
    /// </summary>
    private void OnGetBlurLens(Entity<LensSlotComponent> glasses, ref InventoryRelayedEvent<GetBlurEvent> args)
    {
        if (!_itemSlots.TryGetSlot(glasses.Owner, glasses.Comp.LensSlotId, out var itemSlot))
            return;

        if (!TryComp<VisionCorrectionComponent>(itemSlot.Item, out var component))
            return;

        args.Args.Blur += component.VisionBonus;
        args.Args.CorrectionPower *= component.CorrectionPower;
    }
}
