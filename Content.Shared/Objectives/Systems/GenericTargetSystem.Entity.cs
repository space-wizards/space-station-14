namespace Content.Shared.Objectives.Systems;

/// <summary>
/// This is an abstract system which is inherited from to find and return a hashset of valid minds.
/// </summary>
public abstract partial class EntityTargetSystem : EntitySystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public HashSet<EntityUid> GetMinds(params EntityUid[] exclude)
    {
        var minds = new HashSet<EntityUid>();
        AddMinds(minds, exclude);
        return minds;
    }

    public abstract void AddMinds(HashSet<EntityUid> minds, params EntityUid[] exclude);
}

/// <inheritdoc cref="EntityTargetSystem"/>
public abstract partial class EntityTargetSystem<T> : EntityTargetSystem where T : Component
{
    public override void AddMinds(HashSet<EntityUid> minds, params EntityUid[] exclude)
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (exclude.Contains(uid) || !ValidateEntity((uid, comp)))
                continue;

            minds.Add(uid);
        }
    }

    protected abstract bool ValidateEntity(Entity<T> entity);
}

/// <inheritdoc cref="EntityTargetSystem"/>
public abstract partial class EntityTargetSystem<T1,T2> : EntityTargetSystem
    where T1 : Component
    where T2 : Component
{
    public override void AddMinds(HashSet<EntityUid> minds, params EntityUid[] exclude)
    {
        var query = EntityQueryEnumerator<T1,T2>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (exclude.Contains(uid) || !ValidateEntity((uid, comp1, comp2)))
                continue;

            minds.Add(uid);
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
    public override void AddMinds(HashSet<EntityUid> minds, params EntityUid[] exclude)
    {
        var query = EntityQueryEnumerator<T1,T2,T3>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2, out var comp3))
        {
            if (exclude.Contains(uid) || !ValidateEntity((uid, comp1, comp2, comp3)))
                continue;

            minds.Add(uid);
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
    public override void AddMinds(HashSet<EntityUid> minds, params EntityUid[] exclude)
    {
        var query = EntityQueryEnumerator<T1,T2,T3,T4>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2, out var comp3, out var comp4))
        {
            if (exclude.Contains(uid) || !ValidateEntity((uid, comp1, comp2, comp3, comp4)))
                continue;

            minds.Add(uid);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1,T2,T3,T4> entity);
}
