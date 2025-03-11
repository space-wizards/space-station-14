

using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory.Events;

namespace Content.Client._Starlight.Overlay;

public sealed class FlashImmunityCheck : EntitySystem
{
    [Dependency] private readonly Night.NightVisionSystem _nightVision = default!;
    [Dependency] private readonly Cyclorites.CycloritesVisionSystem _cycloritesVision = default!;
    [Dependency] private readonly Thermal.ThermalVisionSystem _thermalVision = default!;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<FlashImmunityComponent, GotEquippedEvent>(OnFlashImmunityAdded);
        SubscribeLocalEvent<FlashImmunityComponent, GotUnequippedEvent>(OnFlashImmunityRemoved);
    }

    private void OnFlashImmunityAdded(EntityUid uid, FlashImmunityComponent component, GotEquippedEvent args)
    {
        if (TryComp<NightVisionComponent>(args.Equipee, out var nightVisionComponent))
        {
            if (nightVisionComponent.Effect != null)
            {
                nightVisionComponent.blockedByFlashImmunity = true;
                _nightVision.RemoveNightVision(nightVisionComponent);
            }
        }

        if (TryComp<CycloritesVisionComponent>(args.Equipee, out var cycloritesVisionComponent))
        {
            cycloritesVisionComponent.blockedByFlashImmunity = true;
            _cycloritesVision.RemoveNightVision();
        }

        if (TryComp<ThermalVisionComponent>(args.Equipee, out var thermalVisionComponent))
        {
            thermalVisionComponent.blockedByFlashImmunity = true;
            _thermalVision.RemoveNightVision();
        }
    }

    private void OnFlashImmunityRemoved(EntityUid uid, FlashImmunityComponent component, GotUnequippedEvent args)
    {
        if (TryComp<NightVisionComponent>(args.Equipee, out var nightVisionComponent))
        {
            if (nightVisionComponent.Effect == null)
            {
                nightVisionComponent.blockedByFlashImmunity = false;
                _nightVision.AddNightVision(args.Equipee, nightVisionComponent);
            }
        }

        if (TryComp<CycloritesVisionComponent>(args.Equipee, out var cycloritesVisionComponent))
        {
            cycloritesVisionComponent.blockedByFlashImmunity = false;
            _cycloritesVision.AddNightVision(args.Equipee);
        }

        if (TryComp<ThermalVisionComponent>(args.Equipee, out var thermalVisionComponent))
        {
            thermalVisionComponent.blockedByFlashImmunity = false;
            _thermalVision.AddNightVision(args.Equipee);
        }
    }
}
