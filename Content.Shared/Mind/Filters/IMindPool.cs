using Content.Shared.Objectives.Systems;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind pool that can find minds to use for objectives etc.
/// Further filtered by <see cref="IMindFilter"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public partial interface IMindPool
{
    /// <summary>
    /// Add minds for this pool to a hashset.
    /// The hashset gets reused and is cleared before this is called.
    /// </summary>
    /// <param name="minds">The hashset to add to</param>
    /// <param name="dependency">IDependencyCollection, needed to resolve the correct target entity system.</param>
    /// <param name="exclude">A mind entity that must not be returned</param>
    void FindMinds(HashSet<Entity<MindComponent>> minds, IDependencyCollection dependency, params EntityUid[] exclude);
}

/// <summary>
/// A mind pool that can find minds to use for objectives etc.
/// Searches for minds using the corresponding <see cref="MindTargetSystem"/>
/// Further filtered by <see cref="IMindFilter"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class MindPool<T> : IMindPool where T : MindTargetSystem
{
    [Dependency] protected T TargetSystem = default!;

    public void FindMinds(HashSet<Entity<MindComponent>> minds, IDependencyCollection dependency, params EntityUid[] exclude)
    {
        dependency.Resolve(ref TargetSystem);
        TargetSystem.AddMinds(minds, exclude);
    }
}
