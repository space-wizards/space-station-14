using Content.Server.Popups;
using Content.Server.Spawners.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item;
using Content.Shared.Localizations;
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
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<OpenTriggeredStorageFillComponent, ActivateInWorldEvent>(OnOpenEvent);
        SubscribeLocalEvent<OpenTriggeredStorageFillComponent, ExaminedEvent>(OnExamineEvent);
    }

    private void OnExamineEvent(EntityUid uid, OpenTriggeredStorageFillComponent component, ExaminedEvent args)
    {
        args.PushText(Loc.GetString("container-sealed"));
    }

    //Yes, that's a copy of StorageSystem StorageFill method
    private void OnOpenEvent(EntityUid uid, OpenTriggeredStorageFillComponent comp, ActivateInWorldEvent args)
    {
        Log.Debug($"Processing storage fill trigger for entity {ToPrettyString(uid)}");

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
            if (!_storage.Insert(uid, ent, out var remainingEnt, out var reason, playSound: false))
            {
                Log.Error($"Failed to fill {ToPrettyString(uid)} with {ToPrettyString(ent)}. Reason: {reason}");
                // Clean up the spawned entity if insertion fails
                Del(ent);
            }
        }
        _popup.PopupEntity(Loc.GetString("container-unsealed"), args.Target);
        RemComp(uid, comp);
    }

}
