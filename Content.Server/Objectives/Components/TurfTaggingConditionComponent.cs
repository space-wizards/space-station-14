using Content.Server.Objectives.Systems;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Objectives.Components;

/// <summary>
/// Requires that the player spray the most doors for their department.
/// Caches <c>CountAirlocks</c> results to prevent slowdown.
/// </summary>
[RegisterComponent, Access(typeof(TurfTaggingConditionSystem))]
public sealed partial class TurfTaggingConditionComponent : Component
{
    /// <summary>
    /// Doors the best department has.
    /// </summary>
    [DataField]
    public int Best;

    /// <summary>
    /// Doors your department has.
    /// If it's the same as best, you are winning.
    /// </summary>
    [DataField]
    public int Doors;

    /// <summary>
    /// When the cached values were last updated.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan NextCache = TimeSpan.Zero;

    /// <summary>
    /// How long to use cached values before updating them again.
    /// </summary>
    [DataField]
    public TimeSpan CacheExpiry = TimeSpan.FromSeconds(10);
}
