using System.Diagnostics.CodeAnalysis;
using Content.Shared.EntityConditions;
using Content.Shared.Mind;

namespace Content.Shared.Objectives.Systems;

/// <summary>
/// This is an abstract system which is intended to query a list of entities, and acquire a hashset of valid minds from them.
/// It uses EntityQueries to limit the amount of entities which need to be checked, by IComponent.
/// </summary>
public abstract partial class MindTargetSystem : GenericTargetSystem
{
    [Dependency] protected SharedMindSystem Mind = default!;

    public HashSet<Entity<MindComponent>> GetMinds(EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var minds = new HashSet<Entity<MindComponent>>();
        AddMinds(minds, exclude, conditions);
        return minds;
    }

    public abstract void AddMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude = null, params EntityCondition[] conditions);
}

/// <inheritdoc cref="MindTargetSystem"/>
public abstract partial class MindTargetSystem<T> : MindTargetSystem where T : IComponent
{
    public override void AddMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var query = EntityQueryEnumerator<T>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!ValidEntity(uid, exclude, conditions) || !ValidateEntity((uid, comp), out var mind))
                continue;

            minds.Add(mind.Value);
        }
    }

    protected abstract bool ValidateEntity(Entity<T> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind);
}

/// <inheritdoc cref="MindTargetSystem"/>
public abstract partial class MindTargetSystem<T1, T2> : MindTargetSystem
    where T1 : IComponent
    where T2 : IComponent
{
    public override void AddMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var query = EntityQueryEnumerator<T1, T2>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2))
        {
            if (!ValidEntity(uid, exclude, conditions) || !ValidateEntity((uid, comp1, comp2), out var mind))
                continue;

            minds.Add(mind.Value);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1, T2> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind);
}

/// <inheritdoc cref="MindTargetSystem"/>
public abstract partial class MindTargetSystem<T1, T2, T3> : MindTargetSystem
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
{
    public override void AddMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var query = EntityQueryEnumerator<T1, T2, T3>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2, out var comp3))
        {
            if (!ValidEntity(uid, exclude, conditions) || !ValidateEntity((uid, comp1, comp2, comp3), out var mind))
                continue;

            minds.Add(mind.Value);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1, T2, T3> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind);
}

/// <inheritdoc cref="MindTargetSystem"/>
public abstract partial class MindTargetSystem<T1, T2, T3, T4> : MindTargetSystem
    where T1 : IComponent
    where T2 : IComponent
    where T3 : IComponent
    where T4 : IComponent
{
    public override void AddMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        var query = EntityQueryEnumerator<T1, T2, T3, T4>();
        while (query.MoveNext(out var uid, out var comp1, out var comp2, out var comp3, out var comp4))
        {
            if (!ValidEntity(uid, exclude, conditions) || !ValidateEntity((uid, comp1, comp2, comp3, comp4), out var mind))
                continue;

            minds.Add(mind.Value);
        }
    }

    protected abstract bool ValidateEntity(Entity<T1, T2, T3, T4> entity, [NotNullWhen(true)] out Entity<MindComponent>? mind);
}

/// <summary>
/// A mind pool that can find minds to use for objectives etc.
/// Further filtered by <see cref="EntityConditions"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface IMindPool
{
    /// <summary>
    /// Add minds for this pool to a hashset.
    /// The hashset gets reused and is cleared before this is called.
    /// Further filtered by <see cref="EntityConditions"/>.
    /// </summary>
    /// <param name="minds">The hashset to add to</param>
    /// <param name="dependency">IDependencyCollection, needed to resolve the correct target entity system.</param>
    /// <param name="exclude">A mind entity that must not be returned</param>
    /// <param name="conditions">
    /// Optional set of conditions to check each mind against for further filtering.
    /// These conditions must pass for the mind to be valid. </param>
    void FindMinds(HashSet<Entity<MindComponent>> minds, IDependencyCollection dependency, EntityUid? exclude = null, params EntityCondition[] conditions);
}

/// <summary>
/// A mind pool that can find minds to use for objectives etc.
/// Searches for minds using the corresponding <see cref="MindTargetSystem"/>
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class MindPool<T> : IMindPool where T : MindTargetSystem
{
    protected T TargetSystem = default!;

    public void FindMinds(HashSet<Entity<MindComponent>> minds, IDependencyCollection dependency, EntityUid? exclude = null, params EntityCondition[] conditions)
    {
        dependency.Resolve(ref TargetSystem);
        TargetSystem.AddMinds(minds, exclude, conditions);
    }
}
