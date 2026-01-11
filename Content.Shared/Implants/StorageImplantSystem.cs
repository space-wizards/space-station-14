using System.Linq;
using Content.Shared.Implants.Components;
using Content.Shared.Storage;
using Robust.Shared.Containers;
using Robust.Shared.Network;

namespace Content.Shared.Implants;

public sealed class StorageImplantSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StorageImplantComponent, ImplantRemovedEvent>(OnImplantRemoved);
    }

    private void OnImplantRemoved(Entity<StorageImplantComponent> ent, ref ImplantRemovedEvent args)
    {
        if (_net.IsClient)
            return; // TODO: RandomPredicted and DropNextToPredicted

        if (!_container.TryGetContainer(ent.Owner, StorageComponent.ContainerId, out var storageImplant))
            return;

        var contained = storageImplant.ContainedEntities.ToArray();
        foreach (var entity in contained)
        {
            _transform.DropNextTo(entity, ent.Owner);
        }
    }
}
