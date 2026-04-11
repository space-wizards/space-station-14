using Content.Shared.EntityConditions;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// This is an abstract system which is intended to query all entities and return a hashset of valid entities,
/// based on a list of expected components and optional filters.
/// </summary>
public abstract partial class EntityTargetSystem : GenericTargetSystem
{
    public HashSet<EntityUid> GetEntities(EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var entities = new HashSet<EntityUid>();
        AddEntities(entities, exclude);
        return entities;
    }

    public abstract void AddEntities(HashSet<EntityUid> entities, EntityUid? exclude = null, params EntityCondition[] conditions);
}

/// <inheritdoc cref="EntityTargetSystem"/>
public abstract partial class EntityTargetSystem<T> : EntityTargetSystem where T : Component
{
    public override void AddEntities(HashSet<EntityUid> entities, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!ValidEntity(uid, exclude, conditions) || !ValidateEntity((uid, comp)))
                continue;

            entities.Add(uid);
        }
    }

    protected abstract bool ValidateEntity(Entity<T> entity);
}

/// <inheritdoc cref="EntityTargetSystem"/>
public abstract partial class EntityTargetSystem<T1,T2> : EntityTargetSystem
    where T1 : Component
    where T2 : Component
{
    public override void AddEntities(HashSet<EntityUid> entities, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var query = EntityQueryEnumerator<T1,T2>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (!ValidEntity(uid, exclude, conditions) || !ValidateEntity((uid, comp1, comp2)))
                continue;

            entities.Add(uid);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1,T2> entity);
}

/// <inheritdoc cref="EntityTargetSystem"/>
public abstract partial class EntityTargetSystem<T1,T2,T3> : EntityTargetSystem
    where T1 : Component
    where T2 : Component
    where T3 : Component
{
    public override void AddEntities(HashSet<EntityUid> entities, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var query = EntityQueryEnumerator<T1,T2,T3>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2, out var comp3))
        {
            if (!ValidEntity(uid, exclude, conditions)|| !ValidateEntity((uid, comp1, comp2, comp3)))
                continue;

            entities.Add(uid);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1,T2,T3> entity);
}

/// <inheritdoc cref="EntityTargetSystem"/>
public abstract partial class EntityTargetSystem<T1,T2,T3,T4> : EntityTargetSystem
    where T1 : Component
    where T2 : Component
    where T3 : Component
    where T4 : Component
{
    public override void AddEntities(HashSet<EntityUid> entities, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var query = EntityQueryEnumerator<T1,T2,T3,T4>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2, out var comp3, out var comp4))
        {
            if (!ValidEntity(uid, exclude, conditions) || !ValidateEntity((uid, comp1, comp2, comp3, comp4)))
                continue;

            entities.Add(uid);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1,T2,T3,T4> entity);
}
