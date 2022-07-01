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
    [Dependency] private readonly ContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<CursedEntityStorageComponent, ActivateInWorldEvent>(OnActivate,
            after: new[] { typeof(EntityStorageSystem) });
    }

    private void OnActivate(EntityUid uid, CursedEntityStorageComponent component, ActivateInWorldEvent args)
    {
        if (!TryComp<EntityStorageComponent>(uid, out var storage))
            return;

        if (storage.Open || storage.Contents.ContainedEntities.Count <= 0)
            return;

        var lockers = EntityQuery<EntityStorageComponent>().Select(c => c.Owner).ToList();

        if (lockers.Contains(uid))
            lockers.Remove(uid);

        if (lockers.Count == 0)
            return;

        var lockerEnt = _random.Pick(lockers);

        var locker = EntityManager.GetComponent<EntityStorageComponent>(lockerEnt);
        var lockerContainer = locker.Contents;

        foreach (var entity in storage.Contents.ContainedEntities.ToArray())
        {
            storage.Contents.Remove(entity);
            var foo = _entityStorage.AddToContents(entity, lockerEnt, locker);
            Logger.Debug(foo.ToString());
        }
        SoundSystem.Play(component.CursedSound.GetSound(), Filter.Pvs(uid), uid, AudioHelpers.WithVariation(0.125f, _random));
    }
}
