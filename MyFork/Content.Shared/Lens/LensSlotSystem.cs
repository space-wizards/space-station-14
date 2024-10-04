using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Eye.Blinding.Systems;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Robust.Shared.Containers;
using Robust.Shared.Utility;

namespace Content.Shared.Lens;

/// <summary>
///     Manages functionality of <see cref="LensSlotComponent"/>.
///     Enables clothing to proxy the functionality of an item inside of it.
/// </summary>
public sealed class LensSlotSystem : EntitySystem
{
    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;
    [Dependency] private readonly BlurryVisionSystem _BlurryVision = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LensSlotComponent, EntInsertedIntoContainerMessage>(OnLensInserted);
        SubscribeLocalEvent<LensSlotComponent, EntRemovedFromContainerMessage>(OnLensRemoved);

        SubscribeLocalEvent<LensSlotComponent, LensChangedEvent>(OnLensChanged);
        SubscribeLocalEvent<LensSlotComponent, GotEquippedEvent>(OnGlassesEquipped);
        SubscribeLocalEvent<LensSlotComponent, GotUnequippedEvent>(OnGlassesUnequipped);

        SubscribeLocalEvent<LensSlotComponent, ExaminedEvent>(OnExamine);
        SubscribeLocalEvent<LensSlotComponent, InventoryRelayedEvent<GetBlurEvent>>(OnGetBlurLens);
    }

        private void OnLensInserted(EntityUid glasses, LensSlotComponent component, EntInsertedIntoContainerMessage args)
    {
        if (!component.Initialized)
            return;

        if (args.Container.ID != component.LensSlotId)
            return;

        RaiseLocalEvent(glasses, new LensChangedEvent(args.Entity, false));
    }

        private void OnLensRemoved(EntityUid glasses, LensSlotComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.LensSlotId)
            return;

        RaiseLocalEvent(glasses, new LensChangedEvent(args.Entity, true));
    }

    private void OnLensChanged(Entity<LensSlotComponent> glasses, ref LensChangedEvent args)
    {
        var parent = Transform(glasses.Owner).ParentUid;

        if (HasComp<BlindableComponent>(parent) && HasComp<VisionCorrectionComponent>(args.Lens))
            _BlurryVision.UpdateBlurMagnitude(parent);
    }

        private void OnGlassesEquipped(Entity<LensSlotComponent> glasses, ref GotEquippedEvent args)
    {
        if (!_itemSlots.TryGetSlot(glasses.Owner, glasses.Comp.LensSlotId, out var lensSlot))
            return;

        if (HasComp<BlindableComponent>(args.Equipee) && HasComp<VisionCorrectionComponent>(lensSlot.Item))
            _BlurryVision.UpdateBlurMagnitude(args.Equipee);
    }

    private void OnGlassesUnequipped(Entity<LensSlotComponent> glasses, ref GotUnequippedEvent args)
    {
        if (!_itemSlots.TryGetSlot(glasses.Owner, glasses.Comp.LensSlotId, out var lensSlot))
            return;

        if (HasComp<BlindableComponent>(args.Equipee) && HasComp<VisionCorrectionComponent>(lensSlot.Item))
            _BlurryVision.UpdateBlurMagnitude(args.Equipee);
    }

    private void OnExamine(EntityUid glasses, LensSlotComponent component, ref ExaminedEvent args)
    {
        if (!_itemSlots.TryGetSlot(glasses, component.LensSlotId, out var lensSlot))
            return;

        var msg = new FormattedMessage();

        if (lensSlot.Item == null)
            msg.AddMarkupOrThrow(Loc.GetString("lens-empty"));
        else
        {
            var lensName = Loc.GetString(MetaData(lensSlot.Item.Value).EntityName);
            msg.AddMarkupOrThrow(Loc.GetString( "lens-filled", ("lensName", lensName) ));
        }

        args.PushMessage(msg);
    }

        private void OnGetBlurLens(Entity<LensSlotComponent> glasses, ref InventoryRelayedEvent<GetBlurEvent> args)
    {
        if (!_itemSlots.TryGetSlot(glasses.Owner, glasses.Comp.LensSlotId, out var lensSlot))
            return;

        if (!TryComp<VisionCorrectionComponent>(lensSlot.Item, out var component))
            return;

        args.Args.Blur += component.VisionBonus;
        args.Args.CorrectionPower *= component.CorrectionPower;
    }
}
