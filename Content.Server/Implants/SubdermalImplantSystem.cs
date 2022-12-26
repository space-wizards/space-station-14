using Content.Server.Cuffs.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.MobState;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SubdermalImplantComponent, UseFreedomImplantEvent>(OnFreedomImplant);

        SubscribeLocalEvent<ImplantedComponent, MobStateChangedEvent>(RelayToImplantEvent);
        SubscribeLocalEvent<ImplantedComponent, SuicideEvent>(RelayToImplantEvent);
    }

    private void OnFreedomImplant(EntityUid uid, SubdermalImplantComponent component, UseFreedomImplantEvent args)
    {
        if (!TryComp<CuffableComponent>(component.ImplantedEntity, out var cuffs) || cuffs.Container.ContainedEntities.Count < 1)
            return;

        if (TryComp<HandcuffComponent>(cuffs.LastAddedCuffs, out var cuff))
        {
            cuffs.Uncuff(component.ImplantedEntity.Value, cuffs.LastAddedCuffs, cuff, true);
        }
    }

    #region Relays


    //Relays from the implanted to the implant
    private void RelayToImplantEvent<T>(EntityUid uid, ImplantedComponent component, T args) where T : EntityEventArgs
    {
        if (!_container.TryGetContainer(uid, ImplanterComponent.ImplantSlotId, out var implantContainer))
            return;

        foreach (var implant in implantContainer.ContainedEntities)
        {
            RaiseLocalEvent(implant, args);
        }
    }

    //Relays from the implant to the implanted
    private void RelayToImplantedEvent<T>(EntityUid uid, SubdermalImplantComponent component, T args) where T : EntityEventArgs
    {
        if (component.ImplantedEntity != null)
        {
            RaiseLocalEvent(component.ImplantedEntity.Value, args);
        }
    }

    #endregion
}
