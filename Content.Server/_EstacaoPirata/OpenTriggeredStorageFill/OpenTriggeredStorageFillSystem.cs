using Content.Server.Spawners.Components;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Prototypes;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._EstacaoPirata.OpenTriggeredStorageFill;

/// <summary>
/// This handles...
/// </summary>
public sealed class OpenTriggeredStorageFillSystem : EntitySystem
{

    [Dependency] private readonly SharedStorageSystem _storage = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<OpenTriggeredStorageFillComponent, ActivateInWorldEvent>(OnOpenEvent);
    }

    //Yes, that's a copy of StorageSystem StorageFill method
    private void OnOpenEvent(EntityUid uid, OpenTriggeredStorageFillComponent comp, ActivateInWorldEvent args)
    {
        Log.Debug("aaa");
        var coordinates = Transform(uid).Coordinates;

        var spawnItems = EntitySpawnCollection.GetSpawns(comp.Contents);
        foreach (var item in spawnItems)
        {
            DebugTools.Assert(!_prototype.Index<EntityPrototype>(item)
                .HasComponent(typeof(RandomSpawnerComponent)));
            var ent = Spawn(item, coordinates);

            if (!TryComp<ItemComponent>(ent, out var itemComp))
            {
                Log.Error($"Tried to fill {ToPrettyString(uid)} with non-item {item}.");
                Del(ent);
                continue;
            }
            if (!_storage.Insert(uid, ent, out _, out var _, playSound: false))
                Log.Error($"Failed attemp while trying to fill {ToPrettyString(uid)}");
        }

        RemComp(uid, comp);
    }

}
