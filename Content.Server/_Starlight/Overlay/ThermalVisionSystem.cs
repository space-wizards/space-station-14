using Content.Server.Flash;
using Content.Server.Flash.Components;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Inventory.Events;

namespace Content.Server._Starlight.Overlay;
public sealed class VisionsSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;

    public override void Initialize()
    {
        base.Initialize();

        IoCManager.InjectDependencies(this);

        //SubscribeLocalEvent<ThermalVisionComponent, FlashAttemptEvent>(Uncancel, after: [typeof(FlashSystem)]);
        //SubscribeLocalEvent<NightVisionComponent, FlashAttemptEvent>(Uncancel, after: [typeof(FlashSystem)]);
        //SubscribeLocalEvent<CycloritesVisionComponent, FlashAttemptEvent>(Uncancel, after: [typeof(FlashSystem)]);

        SubscribeLocalEvent<FlashImmunityComponent, GotEquippedEvent>(OnFlashImmunityAdded);
        SubscribeLocalEvent<FlashImmunityComponent, GotUnequippedEvent>(OnFlashImmunityRemoved);
    }

    private void OnFlashImmunityAdded(EntityUid uid, FlashImmunityComponent component, GotEquippedEvent args)
    {
        SetVisionComponentsState(args.Equipee, false);
    }

    private void OnFlashImmunityRemoved(EntityUid uid, FlashImmunityComponent component, GotUnequippedEvent args)
    {
        SetVisionComponentsState(args.Equipee, true);
    }

    private void SetVisionComponentsState(EntityUid uid, bool componentState)
    {
        //debug print
        Logger.Info($"Setting vision components state to {componentState}");

        //attempt to disable thermal vision, night vision, and cyclorites vision
        /* if (_entityManager.TryGetComponent<ThermalVisionComponent>(uid, out var thermalVisionComponent))
        {
            thermalVisionComponent.Enabled = componentState;
            Logger.Info($"ThermalVisionComponent found, setting to {componentState}");
        } */

        if (_entityManager.TryGetComponent<NightVisionComponent>(uid, out var nightVisionComponent))
        {
            _entityManager.System<NightVisionSystem>().SetNightVisionState(uid, componentState);
            Logger.Info($"NightVisionComponent found, setting to {componentState}");
        }

        /* if (_entityManager.TryGetComponent<CycloritesVisionComponent>(uid, out var cycloritesVisionComponent))
        {
            cycloritesVisionComponent.Enabled = componentState;
            Logger.Info($"CycloritesVisionComponent found, setting to {componentState}");
        } */
    }
}
