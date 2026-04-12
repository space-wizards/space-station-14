using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Shared.Mind.Filters;

/// <summary>
/// A mind filter that can be used to filter out minds from a <see cref="MindPool"/>.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class MindFilter
{
    /// <summary>
    /// The actual filter function, this has to return false for minds that get removed from the pool.
    /// An excluded mind will be the same one passed to <see cref="MindPool.FindMinds"/>.
    /// </summary>
    /// <param name="mind">The mind to check</param>
    /// <param name="exclude">The same mind passed to FindMinds</param>
    protected abstract bool ShouldRemove(Entity<MindComponent> mind, EntityUid? exclude, IEntityManager entMan);

    /// <summary>
    /// The high-level filter function to be used by the mind system.
    /// </summary>
    public bool Filter(Entity<MindComponent> mind, EntityUid? exclude, EntityManager entMan)
    {
        return ShouldRemove(mind, exclude, entMan) ^ Inverted;
    }

    /// <summary>
    /// Whether to invert functionality, only keeping minds that would otherwise be removed.
    /// </summary>
    [DataField]
    public bool Inverted;
}
