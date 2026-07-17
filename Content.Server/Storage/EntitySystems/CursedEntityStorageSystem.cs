using Content.Server.Storage.Components;
using Content.Shared.Storage.Components;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using System.Linq;

namespace Content.Server.Storage.EntitySystems;

public sealed partial class CursedEntityStorageSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private EntityStorageSystem _entityStorage = default!;
    [Dependency] private SharedAudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedEntityStorageComponent, StorageAfterCloseEvent>(OnClose);
    }

    private void OnClose(EntityUid uid, CursedEntityStorageComponent component, ref StorageAfterCloseEvent args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (storage.Open || storage.Contents.ContainedEntities.Count <= 0)
            return;

        var lockers = new List<Entity<EntityStorageComponent>>();
        var query = EntityQueryEnumerator<EntityStorageComponent>();
        while (query.MoveNext(out var storageUid, out var storageComp))
        {
            lockers.Add((storageUid, storageComp));
        }

        lockers.RemoveAll(e => e.Owner == uid);

        if (lockers.Count == 0)
            return;

        var lockerEnt = _random.Pick(lockers).Owner;

        foreach (var entity in storage.Contents.ContainedEntities.ToArray())
        {
            _container.Remove(entity, storage.Contents);
            _entityStorage.AddToContents(entity, lockerEnt);
        }

        _audio.PlayPvs(component.CursedSound, uid);
    }
}
