using Content.Shared.Objectives.Systems;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind pool that can find minds to use for objectives etc.
/// Further filtered by <see cref="MindFilter"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class MindPool
{
    /// <summary>
    /// Add minds for this pool to a hashset.
    /// The hashset gets reused and is cleared before this is called.
    /// </summary>
    /// <param name="minds">The hashset to add to</param>
    /// <param name="exclude">A mind entity that must not be returned</param>
    /// <param name="entMan">entity Manager for further control</param>
    /// <param name="targetSys">targeting system which explicitly searches for targets.</param>
    public abstract void FindMinds(HashSet<Entity<MindComponent>> minds, EntityUid? exclude, IEntityManager entMan, TargetSystem targetSys);
}
