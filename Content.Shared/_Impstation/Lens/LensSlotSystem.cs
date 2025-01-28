using System.Diagnostics.CodeAnalysis;
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
    /// TODO If this system is expanded outside of <see cref="BlurryVisionSystem"/>, then
    /// the equipped events should find <see cref="VisionCorrectionComponent"/> before calling UpdateBlur.
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
        if (!TryGetLens(glasses, out var lens))
            return;

        _Blurry.UpdateBlurMagnitude(args.Equipee);
    }

    private void OnGlassesUnequipped(Entity<LensSlotComponent> glasses, ref GotUnequippedEvent args)
    {
        if (!TryGetLens(glasses, out var lens))
            return;

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

        RaiseLocalEvent(glasses, new LensChangedEvent(false, args.Entity));
    }

    private void OnLensRemoved(EntityUid glasses, LensSlotComponent component, EntRemovedFromContainerMessage args)
    {
        if (args.Container.ID != component.LensSlotId)
            return;

        RaiseLocalEvent(glasses, new LensChangedEvent(true, args.Entity));
    }

    /// <summary>
    ///     Effectively a copy of <see cref="BlurryVisionSystem.OnGetBlur"/>.
    /// </summary>
    private void OnGetBlurLens(Entity<LensSlotComponent> glasses, ref InventoryRelayedEvent<GetBlurEvent> args)
    {
        if (!TryGetLens(glasses, out var lens))
            return;

        if (!TryComp<VisionCorrectionComponent>(lens, out var component))
            return;

        args.Args.Blur += component.VisionBonus;
        args.Args.CorrectionPower *= component.CorrectionPower;
    }

    /// <summary>
    ///     Attempt to get the lens from the lens slot.
    /// </summary>
    private bool TryGetLens(Entity<LensSlotComponent> glasses, [NotNullWhen(true)] out EntityUid? lens)
    {
        lens = _itemSlots.GetItemOrNull(glasses.Owner, glasses.Comp.LensSlotId);

        if (lens == null)
            return false;
        else
            return true;
    }
}
