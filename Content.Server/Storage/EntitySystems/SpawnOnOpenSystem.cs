using Content.Server.Storage.Components;
using Content.Shared.Storage;
using Content.Shared.Storage.Components;
using Robust.Shared.Containers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server.Storage.EntitySystems;

/// <summary>
/// This system handles spawning entities when a storage container is opened.
/// </summary>
public sealed class SpawnOnOpenSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityStorageSystem _entityStorage = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;

    public override void Initialize()
    {
        base.Initialize();
        
        SubscribeLocalEvent<SpawnOnOpenComponent, StorageAfterOpenEvent>(OnStorageOpen);
    }

    private void OnStorageOpen(EntityUid uid, SpawnOnOpenComponent component, ref StorageAfterOpenEvent args)
    {
        // Only spawn once
        if (component.HasSpawned)
            return;
            
        if (component.Prototypes.Count == 0)
            return;

        // Check chance
        if (component.Chance < 1.0f && !_random.Prob(component.Chance))
            return;

        // Select a random prototype
        var prototype = _random.Pick(component.Prototypes);
        
        // Spawn the entity inside the storage
        if (TryComp<EntityStorageComponent>(uid, out var storage))
        {
            var entity = Spawn(prototype, Transform(uid).Coordinates);
            _entityStorage.Insert(entity, uid, storage);
            
            // Mark as spawned so it doesn't spawn again
            component.HasSpawned = true;
        }
    }
}
