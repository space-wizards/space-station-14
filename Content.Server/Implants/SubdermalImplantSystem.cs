using System.Linq;
using Content.Server.Explosion.Components;
using Content.Server.Explosion.EntitySystems;
using Content.Shared.Body.Components;
using Content.Shared.Implants;
using Content.Shared.Implants.Components;
using Content.Shared.MobState;
using Content.Shared.Verbs;
using Robust.Shared.Containers;

namespace Content.Server.Implants;

public sealed class SubdermalImplantSystem : SharedSubdermalImplantSystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly TriggerSystem _trigger = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ImplantedComponent, MobStateChangedEvent>(RelayImplantEvent);
        //TODO: Revisit when StorageSystem is reworked or there's a way to get the storage verb onto a prototype
        //TODO: Revisit when chain triggering is a thing to get a timer to work on macrobomb
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
