using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory.Events;
using Content.Shared.Inventory;

namespace Content.Shared.Eye.Blinding.Systems;

public sealed class BlurryVisionSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlindableComponent, EyeDamageChangedEvent>(OnDamageChanged);
        SubscribeLocalEvent<VisionCorrectionComponent, GotEquippedEvent>(OnGlassesEquipped);
        SubscribeLocalEvent<VisionCorrectionComponent, GotUnequippedEvent>(OnGlassesUnequipped);
        SubscribeLocalEvent<VisionCorrectionComponent, InventoryRelayedEvent<GetBlurEvent>>(OnGetBlur);
    }

    private void OnGetBlur(EntityUid uid, VisionCorrectionComponent component, InventoryRelayedEvent<GetBlurEvent> args)
    {
        args.Args.Blur += component.VisionBonus;
    }

    private void OnDamageChanged(EntityUid uid, BlindableComponent component, ref EyeDamageChangedEvent args)
    {
        UpdateBlurMagnitude(uid, component);
    }

    private void UpdateBlurMagnitude(EntityUid uid, BlindableComponent? component = null)
    {
        if (!Resolve(uid, ref component, false))
            return;

        var ev = new GetBlurEvent(component.EyeDamage);
        RaiseLocalEvent(uid, ev);

        var blur = Math.Clamp(0, ev.Blur, BlurryVisionComponent.MaxMagnitude);
        if (blur <= 0)
        {
            RemCompDeferred<BlurryVisionComponent>(uid);
            return;
        }

        var blurry = EnsureComp<BlurryVisionComponent>(uid);
        blurry.Magnitude = blur;
        Dirty(blurry);
    }

    private void OnGlassesEquipped(EntityUid uid, VisionCorrectionComponent component, GotEquippedEvent args)
    {
        UpdateBlurMagnitude(uid);
    }

    private void OnGlassesUnequipped(EntityUid uid, VisionCorrectionComponent component, GotUnequippedEvent args)
    {
        UpdateBlurMagnitude(uid);
    }
}

public sealed class GetBlurEvent : EntityEventArgs, IInventoryRelayEvent
{
    public readonly float BaseBlur;
    public float Blur;

    public GetBlurEvent(float blur)
    {
        Blur = blur;
        BaseBlur = blur;
    }

    public SlotFlags TargetSlots => SlotFlags.HEAD | SlotFlags.MASK | SlotFlags.EYES;
}
