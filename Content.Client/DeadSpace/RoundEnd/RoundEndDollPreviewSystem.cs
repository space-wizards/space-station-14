// Мёртвый Космос, Licensed under custom terms with restrictions on public hosting and commercial use, full text: https://raw.githubusercontent.com/dead-space-server/space-station-14-fobos/master/LICENSE.TXT

using System.Linq;
using Content.Client.Humanoid;
using Content.Client.Inventory;
using Content.Shared.Chat.TypingIndicator;
using Content.Shared.DeadSpace.RoundEnd;
using Content.Shared.GameTicking;
using Content.Shared.Humanoid;
using Content.Shared.Humanoid.Prototypes;
using Content.Shared.Inventory;
using Content.Shared.Roles;
using Robust.Client.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Client.DeadSpace.RoundEnd;

/// <summary>
/// Builds at most one local manifest doll per client frame and caches one immutable sprite per player.
/// </summary>
public sealed class RoundEndDollPreviewSystem : EntitySystem
{
    private const string SnapshotPrototype = "clientsideclone";
    private const string FallbackPrototype = "MobObserver";

    [Dependency] private readonly ClientInventorySystem _inventory = default!;
    [Dependency] private readonly HumanoidAppearanceSystem _humanoid = default!;
    [Dependency] private readonly IPrototypeManager _prototypes = default!;
    [Dependency] private readonly SpriteSystem _sprites = default!;

    private readonly Dictionary<int, OwnerState> _owners = new();
    private readonly Queue<BuildRequest> _queue = new();
    private int _nextOwner;
    private bool _rebuildOwnersNextUpdate;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeNetworkEvent<RoundRestartCleanupEvent>(_ => ScheduleRebuildAfterCleanup());
    }

    public int CreateOwner()
    {
        var owner = ++_nextOwner;
        _owners.Add(owner, new OwnerState());
        return owner;
    }

    public void Request(int owner, RoundEndDollData data, Action<EntityUid?> callback)
    {
        if (!_owners.TryGetValue(owner, out var state))
            return;

        if (state.Entries.TryGetValue(data, out var existing))
        {
            // Keep subscribers for the lifetime of the owner so cached sprites can be rebuilt
            // after the client clears round entities and enters the lobby.
            existing.Callbacks.Add(callback);
            if (existing.Ready)
                callback(existing.Snapshot);

            return;
        }

        var entry = new PreviewEntry();
        entry.Callbacks.Add(callback);
        state.Entries.Add(data, entry);
        _queue.Enqueue(new BuildRequest(owner, data, entry));
    }

    public void Cancel(int owner)
    {
        if (!_owners.Remove(owner, out var state))
            return;

        var queued = _queue.Count;
        for (var i = 0; i < queued; i++)
        {
            var request = _queue.Dequeue();
            if (request.Owner != owner)
                _queue.Enqueue(request);
        }

        foreach (var entry in state.Entries.Values)
        {
            if (entry.Snapshot is { } snapshot && !Deleted(snapshot))
                Del(snapshot);

            entry.Callbacks.Clear();
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (_rebuildOwnersNextUpdate)
        {
            _rebuildOwnersNextUpdate = false;
            QueueOwnerRebuilds();
            return;
        }

        while (_queue.TryDequeue(out var request))
        {
            if (!_owners.TryGetValue(request.Owner, out var state) ||
                !state.Entries.TryGetValue(request.Data, out var current) ||
                !ReferenceEquals(current, request.Entry))
            {
                continue;
            }

            EntityUid? snapshot = null;
            try
            {
                snapshot = BuildSnapshot(request.Data);
            }
            catch (Exception e)
            {
                Log.Warning($"Failed to build a round-end doll: {e}");
            }

            request.Entry.Snapshot = snapshot;
            request.Entry.Ready = true;
            foreach (var callback in request.Entry.Callbacks)
                callback(snapshot);

            break;
        }
    }

    public override void Shutdown()
    {
        ClearAll();
        base.Shutdown();
    }

    private EntityUid? BuildSnapshot(RoundEndDollData data)
    {
        EntityUid? body = null;
        EntityUid? snapshot = null;
        var spawnedItems = new List<EntityUid>();

        try
        {
            body = TrySpawnBody(data);
            if (body == null || !TryComp<SpriteComponent>(body.Value, out var bodySprite))
                return null;

            if (data.Humanoid != null && HasComp<HumanoidAppearanceComponent>(body.Value))
                _humanoid.ApplyRoundEndAppearance(body.Value, data.Humanoid);

            if (data.Equipment.Length > 0)
                EquipRecordedItems(body.Value, data.Equipment, spawnedItems);
            else if (data.FallbackGear != null)
                EquipFallbackGear(body.Value, data.FallbackGear.Value, spawnedItems);

            _sprites.ForceUpdate(body.Value);
            if (!HasDrawableSprite(bodySprite, GetTypingIndicatorLayer(body.Value, bodySprite)))
                return null;

            snapshot = Spawn(SnapshotPrototype, MapCoordinates.Nullspace);
            var snapshotSprite = Comp<SpriteComponent>(snapshot.Value);
            _sprites.CopySprite((body.Value, bodySprite), (snapshot.Value, snapshotSprite));
            HideTypingIndicator(snapshot.Value, snapshotSprite);
            _sprites.SetRotation((snapshot.Value, snapshotSprite), Angle.Zero);
            _sprites.ForceUpdate(snapshot.Value);

            if (HasDrawableSprite(snapshotSprite))
            {
                var result = snapshot;
                snapshot = null;
                return result;
            }

            return null;
        }
        finally
        {
            if (snapshot != null && !Deleted(snapshot.Value))
                Del(snapshot.Value);

            if (body != null && !Deleted(body.Value))
                Del(body.Value);

            foreach (var item in spawnedItems)
            {
                if (!Deleted(item))
                    Del(item);
            }
        }
    }

    private EntityUid? TrySpawnBody(RoundEndDollData data)
    {
        if (data.BodyPrototype is { } bodyPrototype && _prototypes.HasIndex<EntityPrototype>(bodyPrototype))
        {
            if (TrySpawnDrawableBody(bodyPrototype, out var body))
                return body;
        }

        if (data.Humanoid != null &&
            _prototypes.TryIndex(data.Humanoid.Species, out SpeciesPrototype? species) &&
            _prototypes.HasIndex<EntityPrototype>(species.DollPrototype))
        {
            if (TrySpawnDrawableBody(species.DollPrototype, out var body))
                return body;
        }

        return TrySpawnDrawableBody(FallbackPrototype, out var fallback)
            ? fallback
            : null;
    }

    private bool TrySpawnDrawableBody(EntProtoId prototype, out EntityUid body)
    {
        body = default;
        if (!_prototypes.HasIndex<EntityPrototype>(prototype))
            return false;

        try
        {
            body = Spawn(prototype, MapCoordinates.Nullspace);
            if (HasComp<SpriteComponent>(body))
                return true;

            Del(body);
            body = default;
        }
        catch (Exception e)
        {
            Log.Warning($"Unable to spawn manifest body prototype {prototype}: {e.Message}");
            if (body.IsValid() && !Deleted(body))
                Del(body);

            body = default;
        }

        return false;
    }

    private void EquipRecordedItems(
        EntityUid body,
        RoundEndDollEquipment[] equipment,
        List<EntityUid> spawnedItems)
    {
        foreach (var entry in equipment)
        {
            if (!_prototypes.HasIndex<EntityPrototype>(entry.Prototype))
                continue;

            TryEquip(body, entry.Slot, entry.Prototype, spawnedItems);
        }
    }

    private void EquipFallbackGear(
        EntityUid body,
        ProtoId<StartingGearPrototype> gearId,
        List<EntityUid> spawnedItems)
    {
        if (!_prototypes.TryIndex(gearId, out StartingGearPrototype? gear) ||
            !_inventory.TryGetSlots(body, out var slots))
        {
            return;
        }

        foreach (var slot in slots)
        {
            var prototype = ((IEquipmentLoadout) gear).GetGear(slot.Name);
            if (string.IsNullOrEmpty(prototype) || !_prototypes.HasIndex<EntityPrototype>(prototype))
                continue;

            TryEquip(body, slot.Name, prototype, spawnedItems);
        }
    }

    private void TryEquip(EntityUid body, string slot, EntProtoId prototype, List<EntityUid> spawnedItems)
    {
        EntityUid? item = null;
        try
        {
            item = Spawn(prototype, MapCoordinates.Nullspace);
            spawnedItems.Add(item.Value);
            if (!_inventory.TryEquip(body, item.Value, slot, true, true))
                Del(item.Value);
        }
        catch (Exception e)
        {
            Log.Warning($"Unable to equip manifest item {prototype} in {slot}: {e.Message}");
            if (item != null && !Deleted(item.Value))
                Del(item.Value);
        }
    }

    private int? GetTypingIndicatorLayer(EntityUid uid, SpriteComponent sprite)
    {
        return _sprites.LayerMapTryGet((uid, sprite), TypingIndicatorLayers.Base, out var layer, false)
            ? layer
            : null;
    }

    private void HideTypingIndicator(EntityUid uid, SpriteComponent sprite)
    {
        if (_sprites.LayerMapTryGet((uid, sprite), TypingIndicatorLayers.Base, out var layer, false))
            _sprites.LayerSetVisible((uid, sprite), layer, false);
    }

    private static bool HasDrawableSprite(SpriteComponent sprite, int? ignoredLayer = null)
    {
        if (!sprite.Visible)
            return false;

        var index = 0;
        foreach (var layer in sprite.AllLayers)
        {
            if (ignoredLayer == index++)
                continue;

            if (layer.Visible && (layer.Texture != null || layer.RsiState.IsValid && layer.ActualRsi != null))
                return true;
        }

        return false;
    }

    private void ScheduleRebuildAfterCleanup()
    {
        _queue.Clear();

        foreach (var state in _owners.Values)
        {
            foreach (var entry in state.Entries.Values)
            {
                if (entry.Snapshot is { } snapshot && !Deleted(snapshot))
                    Del(snapshot);

                entry.Snapshot = null;
                entry.Ready = false;
            }
        }

        // Wait one update for the rest of client round cleanup before queuing replacement snapshots.
        _rebuildOwnersNextUpdate = _owners.Count > 0;
    }

    private void QueueOwnerRebuilds()
    {
        _queue.Clear();
        foreach (var (owner, state) in _owners)
        {
            foreach (var (data, entry) in state.Entries)
                _queue.Enqueue(new BuildRequest(owner, data, entry));
        }
    }

    private void ClearAll()
    {
        _rebuildOwnersNextUpdate = false;
        foreach (var owner in _owners.Keys.ToArray())
            Cancel(owner);

        _queue.Clear();
    }

    private sealed class OwnerState
    {
        public readonly Dictionary<RoundEndDollData, PreviewEntry> Entries =
            new(ReferenceEqualityComparer.Instance);
    }

    private sealed class PreviewEntry
    {
        public readonly List<Action<EntityUid?>> Callbacks = new();
        public EntityUid? Snapshot;
        public bool Ready;
    }

    private readonly record struct BuildRequest(int Owner, RoundEndDollData Data, PreviewEntry Entry);
}
