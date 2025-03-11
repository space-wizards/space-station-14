

using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;
using Content.Shared.Inventory.Events;

namespace Content.Client._Starlight.Overlay;

public sealed class FlashImmunityCheck : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    public override void Initialize()
    {
        base.Initialize();
        IoCManager.InjectDependencies(this);

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
                _entityManager.System<Night.NightVisionSystem>().RemoveNightVision(nightVisionComponent);
            }
        }

        if (TryComp<CycloritesVisionComponent>(args.Equipee, out var cycloritesVisionComponent))
        {
            cycloritesVisionComponent.blockedByFlashImmunity = true;
            _entityManager.System<Cyclorites.CycloritesVisionSystem>().RemoveNightVision();
        }

        if (TryComp<ThermalVisionComponent>(args.Equipee, out var thermalVisionComponent))
        {
            thermalVisionComponent.blockedByFlashImmunity = true;
            _entityManager.System<Thermal.ThermalVisionSystem>().RemoveNightVision();
        }
    }

    private void OnFlashImmunityRemoved(EntityUid uid, FlashImmunityComponent component, GotUnequippedEvent args)
    {
        if (TryComp<NightVisionComponent>(args.Equipee, out var nightVisionComponent))
        {
            if (nightVisionComponent.Effect == null)
            {
                nightVisionComponent.blockedByFlashImmunity = false;
                _entityManager.System<Night.NightVisionSystem>().AddNightVision(args.Equipee, nightVisionComponent);
            }
        }

        if (TryComp<CycloritesVisionComponent>(args.Equipee, out var cycloritesVisionComponent))
        {
            cycloritesVisionComponent.blockedByFlashImmunity = false;
            _entityManager.System<Cyclorites.CycloritesVisionSystem>().AddNightVision(args.Equipee);
        }

        if (TryComp<ThermalVisionComponent>(args.Equipee, out var thermalVisionComponent))
        {
            thermalVisionComponent.blockedByFlashImmunity = false;
            _entityManager.System<Thermal.ThermalVisionSystem>().AddNightVision(args.Equipee);
        }
    }
}
