using Content.Shared.Containers.ItemSlots;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory;
using Content.Shared.Inventory.Events;
using Content.Shared.Lens;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class BlurryVisionSystem : EntitySystem
{

    [Dependency] private readonly ItemSlotsSystem _itemSlots = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VisionCorrectionComponent, GotEquippedEvent>(OnGlassesEquipped);
        SubscribeLocalEvent<VisionCorrectionComponent, GotUnequippedEvent>(OnGlassesUnequipped);
        SubscribeLocalEvent<VisionCorrectionComponent, InventoryRelayedEvent<GetBlurEvent>>(OnGetBlur);

        SubscribeLocalEvent<LensSlotComponent, GotEquippedEvent>(OnLensEquipped);
        SubscribeLocalEvent<LensSlotComponent, GotUnequippedEvent>(OnLensUnequipped);
        SubscribeLocalEvent<LensSlotComponent, LensChangedEvent>(OnLensChanged);
        SubscribeLocalEvent<LensSlotComponent, InventoryRelayedEvent<GetBlurEvent>>(OnGetBlurLens);
    }

    private void OnGetBlur(Entity<VisionCorrectionComponent> glasses, ref InventoryRelayedEvent<GetBlurEvent> args)
    {
        args.Args.Blur += glasses.Comp.VisionBonus;
        args.Args.CorrectionPower *= glasses.Comp.CorrectionPower;
    }

        private void OnGetBlurLens(Entity<LensSlotComponent> glasses, ref InventoryRelayedEvent<GetBlurEvent> args)
    {
        if (!_itemSlots.TryGetSlot(glasses.Owner, glasses.Comp.LensSlotId, out var itemSlot))
            return;

        if (!TryComp<VisionCorrectionComponent>(itemSlot.Item, out var component))
            return;

        args.Args.Blur += component.VisionBonus;
        args.Args.CorrectionPower *= component.CorrectionPower;
    }

    public void UpdateBlurMagnitude(Entity<BlindableComponent?> ent)
    {
        if (!Resolve(ent.Owner, ref ent.Comp, false))
            return;

        var ev = new GetBlurEvent(ent.Comp.EyeDamage);
        RaiseLocalEvent(ent, ev);

        var blur = Math.Clamp(ev.Blur, 0, BlurryVisionComponent.MaxMagnitude);
        if (blur <= 0)
        {
            RemCompDeferred<BlurryVisionComponent>(ent);
            return;
        }

        var blurry = EnsureComp<BlurryVisionComponent>(ent);
        blurry.Magnitude = blur;
        blurry.CorrectionPower = ev.CorrectionPower;
        Dirty(ent, blurry);
    }

    private void OnGlassesEquipped(Entity<VisionCorrectionComponent> glasses, ref GotEquippedEvent args)
    {
        UpdateBlurMagnitude(args.Equipee);
    }

    private void OnGlassesUnequipped(Entity<VisionCorrectionComponent> glasses, ref GotUnequippedEvent args)
    {
        UpdateBlurMagnitude(args.Equipee);
    }

        private void OnLensEquipped(Entity<LensSlotComponent> glasses, ref GotEquippedEvent args)
    {
        UpdateBlurMagnitude(args.Equipee);
    }

    private void OnLensUnequipped(Entity<LensSlotComponent> glasses, ref GotUnequippedEvent args)
    {
        UpdateBlurMagnitude(args.Equipee);
    }

    private void OnLensChanged(Entity<LensSlotComponent> glasses, ref LensChangedEvent args)
    {
        UpdateBlurMagnitude(Transform(glasses.Owner).ParentUid);
    }
}

public sealed class GetBlurEvent : EntityEventArgs, IInventoryRelayEvent
{
    public readonly float BaseBlur;
    public float Blur;
    public float CorrectionPower = BlurryVisionComponent.DefaultCorrectionPower;

    public GetBlurEvent(float blur)
    {
        Blur = blur;
        BaseBlur = blur;
    }

    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.EYES;
}
