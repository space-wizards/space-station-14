using System.Diagnostics.CodeAnalysis;
using Content.Shared.Mind;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// This is an abstract system which is inherited from to find and return a hashset of valid minds.
/// </summary>
public abstract partial class MindTargetSystem : EntitySystem
{
    [Dependency] protected readonly SharedMindSystem Mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public HashSet<Entity<MindComponent>> GetMinds(params EntityUid[] exclude)
    {
        var minds = new HashSet<Entity<MindComponent>>();
        AddMinds(minds, exclude);
        return minds;
    }

    public abstract void AddMinds(HashSet<Entity<MindComponent>> minds, params EntityUid[] exclude);
}


/// <inheritdoc cref="MindTargetSystem"/>
public abstract partial class MindTargetSystem<T> : MindTargetSystem where T : Component
{
    public override void AddMinds(HashSet<Entity<MindComponent>> minds, params EntityUid[] exclude)
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!ValidateEntity((uid, comp), out var mind) || exclude.Contains(mind.Value))
                continue;

            minds.Add(mind.Value);
        }
    }

    protected abstract bool ValidateEntity(Entity<T> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind);
}

/// <inheritdoc cref="MindTargetSystem"/>
public abstract partial class MindTargetSystem<T1,T2> : MindTargetSystem
    where T1 : Component
    where T2 : Component
{
    public override void AddMinds(HashSet<Entity<MindComponent>> minds, params EntityUid[] exclude)
    {
        var query = EntityQueryEnumerator<T1,T2>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (!ValidateEntity((uid, comp1, comp2), out var mind) || exclude.Contains(mind.Value))
                continue;

            minds.Add(mind.Value);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1,T2> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind);
}

/// <inheritdoc cref="MindTargetSystem"/>
public abstract partial class MindTargetSystem<T1,T2,T3> : MindTargetSystem
    where T1 : Component
    where T2 : Component
    where T3 : Component
{
    public override void AddMinds(HashSet<Entity<MindComponent>> minds, params EntityUid[] exclude)
    {
        var query = EntityQueryEnumerator<T1,T2,T3>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2, out var comp3))
        {
            if (!ValidateEntity((uid, comp1, comp2, comp3), out var mind) || exclude.Contains(mind.Value))
                continue;

            minds.Add(mind.Value);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1,T2,T3> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind);
}

/// <inheritdoc cref="MindTargetSystem"/>
public abstract partial class MindTargetSystem<T1,T2,T3,T4> : MindTargetSystem
    where T1 : Component
    where T2 : Component
    where T3 : Component
    where T4 : Component
{
    public override void AddMinds(HashSet<Entity<MindComponent>> minds, params EntityUid[] exclude)
    {
        var query = EntityQueryEnumerator<T1,T2,T3,T4>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2, out var comp3, out var comp4))
        {
            if (!ValidateEntity((uid, comp1, comp2, comp3, comp4), out var mind) || exclude.Contains(mind.Value))
                continue;

            minds.Add(mind.Value);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1,T2,T3,T4> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind);
}
