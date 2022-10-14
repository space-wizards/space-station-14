using Content.Server.Light.Events;
using Content.Shared.Actions;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.MobState;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantedComponent, MobStateChangedEvent>(RelayImplantEvent);
        //SubscribeLocalEvent<ImplantedComponent, GetVerbsEvent<ActivationVerb>>(RelayImplantEvent); //see if this can get storage working
        //TODO: Revisit when StorageSystem is reworked or there's a way to get the storage verb onto a prototype
    }

    private void RelayImplantEvent<T>(EntityUid uid, ImplantedComponent component, T args) where T : EntityEventArgs
    {
        if (!_container.TryGetContainer(uid, ImplantSlotId, out var implantContainer))
            return;

        foreach (var implant in implantContainer.ContainedEntities)
        {
            RaiseLocalEvent(implant, args);
        }
    }
}
