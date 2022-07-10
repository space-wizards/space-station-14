using Content.Server.Storage.Components;
using Content.Shared.Audio;
using Content.Shared.Interaction;
using Robust.Server.Containers;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Storage.EntitySystems;

public sealed class CursedEntityStorageSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedEntityStorageComponent, StorageAfterCloseEvent>(OnClose);
    }

    private void OnClose(EntityUid uid, CursedEntityStorageComponent component, StorageAfterCloseEvent args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (storage.Open || storage.Contents.ContainedEntities.Count <= 0)
            return;

        var lockerQuery = EntityQuery<EntityStorageComponent>().ToList();
        lockerQuery.Remove(storage);

        if (lockerQuery.Count == 0)
            return;

        var lockerEnt = _random.Pick(lockerQuery).Owner;

        foreach (var entity in storage.Contents.ContainedEntities.ToArray())
        {
            storage.Contents.Remove(entity);
            _entityStorage.AddToContents(entity, lockerEnt);
        }
        SoundSystem.Play(component.CursedSound.GetSound(), Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.125f, _random));
    }
}
