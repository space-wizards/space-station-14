using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Shared.Stacks;

// Partial for spawning stacks.
public abstract partial class SharedStackSystem
{
    /// <summary>
    /// Spawns a stack of a certain stack type and sets its count. Won't set the stack over its max.
    /// </summary>
    /// <param name="count">The amount to set the spawned stack to.</param>
    /// <param name="prototype"></param>
    /// <param name="spawnPosition"></param>
    [PublicAPI]
    public EntityUid SpawnAtPosition(int count, StackPrototype prototype, EntityCoordinates spawnPosition)
    {
        var entity = PredictedSpawnAtPosition(prototype.Spawn, spawnPosition); // The real SpawnAtPosition

        SetCount((entity, null), count);
        return entity;
    }

    /// <inheritdoc cref="SpawnAtPosition(int, StackPrototype, EntityCoordinates)"/>
    [PublicAPI]
    public EntityUid SpawnAtPosition(int count, ProtoId<StackPrototype> id, EntityCoordinates spawnPosition)
    {
        var proto = _prototype.Index(id);
        return SpawnAtPosition(count, proto, spawnPosition);
    }

    /// <summary>
    /// Say you want to spawn 97 units of something that has a max stack count of 30.
    /// This would spawn 3 stacks of 30 and 1 stack of 7.
    /// </summary>
    /// <returns>The entities spawned.</returns>
    /// <remarks> If the entity to spawn doesn't have stack component this will spawn a bunch of single items. </remarks>
    private List<EntityUid> SpawnMultipleAtPosition(EntProtoId entityPrototype,
        List<int> amounts,
        EntityCoordinates spawnPosition)
    {
        if (amounts.Count <= 0)
        {
            Log.Error(
                $"Attempted to spawn stacks of nothing: {entityPrototype}, {amounts}. Trace: {Environment.StackTrace}");
            return new();
        }

        var spawnedEnts = new List<EntityUid>();
        foreach (var count in amounts)
        {
            var entity = PredictedSpawnAtPosition(entityPrototype, spawnPosition); // The real SpawnAtPosition
            spawnedEnts.Add(entity);
            if (TryComp<StackComponent>(entity, out var stackComp)) // prevent errors from the Resolve
                SetCount((entity, stackComp), count);
        }

        return spawnedEnts;
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleAtPosition(EntProtoId entityPrototypeId,
        int amount,
        EntityCoordinates spawnPosition)
    {
        return SpawnMultipleAtPosition(entityPrototypeId,
            CalculateSpawns(entityPrototypeId, amount),
            spawnPosition);
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleAtPosition(EntityPrototype entityProto,
        int amount,
        EntityCoordinates spawnPosition)
    {
        return SpawnMultipleAtPosition(entityProto.ID,
            CalculateSpawns(entityProto, amount),
            spawnPosition);
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleAtPosition(StackPrototype stack,
        int amount,
        EntityCoordinates spawnPosition)
    {
        return SpawnMultipleAtPosition(stack.Spawn,
            CalculateSpawns(stack, amount),
            spawnPosition);
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleAtPosition(ProtoId<StackPrototype> stackId,
        int amount,
        EntityCoordinates spawnPosition)
    {
        var stackProto = _prototype.Index(stackId);
        return SpawnMultipleAtPosition(stackProto.Spawn,
            CalculateSpawns(stackProto, amount),
            spawnPosition);
    }

    /// <inheritdoc cref="SpawnAtPosition(int, StackPrototype, EntityCoordinates)"/>
    [PublicAPI]
    public EntityUid SpawnNextToOrDrop(int amount, StackPrototype prototype, EntityUid source)
    {
        var entity = PredictedSpawnNextToOrDrop(prototype.Spawn, source); // The real SpawnNextToOrDrop
        SetCount((entity, null), amount);
        return entity;
    }

    /// <inheritdoc cref="SpawnNextToOrDrop(int, StackPrototype, EntityUid)"/>
    [PublicAPI]
    public EntityUid SpawnNextToOrDrop(int amount, ProtoId<StackPrototype> id, EntityUid source)
    {
        var proto = _prototype.Index(id);
        return SpawnNextToOrDrop(amount, proto, source);
    }

    /// <inheritdoc cref="SpawnMultipleAtPosition(EntProtoId, List{int}, EntityCoordinates)"/>
    private List<EntityUid> SpawnMultipleNextToOrDrop(EntProtoId entityPrototype,
        List<int> amounts,
        EntityUid target)
    {
        if (amounts.Count <= 0)
        {
            Log.Error(
                $"Attempted to spawn stacks of nothing: {entityPrototype}, {amounts}. Trace: {Environment.StackTrace}");
            return new();
        }

        var spawnedEnts = new List<EntityUid>();
        foreach (var count in amounts)
        {
            var entity = PredictedSpawnNextToOrDrop(entityPrototype, target); // The real SpawnNextToOrDrop
            spawnedEnts.Add(entity);
            if (TryComp<StackComponent>(entity, out var stackComp)) // prevent errors from the Resolve
                SetCount((entity, stackComp), count);
        }

        return spawnedEnts;
    }

    /// <inheritdoc cref="SpawnMultipleNextToOrDrop(EntProtoId, List{int}, EntityUid)"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleNextToOrDrop(EntProtoId stack,
        int amount,
        EntityUid target)
    {
        return SpawnMultipleNextToOrDrop(stack,
            CalculateSpawns(stack, amount),
            target);
    }

    /// <inheritdoc cref="SpawnMultipleNextToOrDrop(EntProtoId, List{int}, EntityUid)"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleNextToOrDrop(EntityPrototype stack,
        int amount,
        EntityUid target)
    {
        return SpawnMultipleNextToOrDrop(stack.ID,
            CalculateSpawns(stack, amount),
            target);
    }

    /// <inheritdoc cref="SpawnMultipleNextToOrDrop(EntProtoId, List{int}, EntityUid)"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleNextToOrDrop(StackPrototype stack,
        int amount,
        EntityUid target)
    {
        return SpawnMultipleNextToOrDrop(stack.Spawn,
            CalculateSpawns(stack, amount),
            target);
    }

    /// <inheritdoc cref="SpawnMultipleNextToOrDrop(EntProtoId, List{int}, EntityUid)"/>
    [PublicAPI]
    public List<EntityUid> SpawnMultipleNextToOrDrop(ProtoId<StackPrototype> stackId,
        int amount,
        EntityUid target)
    {
        var stackProto = _prototype.Index(stackId);
        return SpawnMultipleNextToOrDrop(stackProto.Spawn,
            CalculateSpawns(stackProto, amount),
            target);
    }

    /// <inheritdoc cref="CalculateSpawns(int, int)"/>
    private List<int> CalculateSpawns(StackPrototype stackProto, int amount)
    {
        return CalculateSpawns(GetMaxCount(stackProto), amount);
    }

    /// <inheritdoc cref="CalculateSpawns(int, int)"/>
    private List<int> CalculateSpawns(EntityPrototype entityPrototype, int amount)
    {
        return CalculateSpawns(GetMaxCount(entityPrototype), amount);
    }

    /// <inheritdoc cref="CalculateSpawns(int, int)"/>
    private List<int> CalculateSpawns(EntProtoId entityId, int amount)
    {
        return CalculateSpawns(GetMaxCount(entityId), amount);
    }
}
