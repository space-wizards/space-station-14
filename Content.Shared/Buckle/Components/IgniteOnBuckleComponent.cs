using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Buckle.Components;

/// <summary>
/// Component that makes an entity ignite entities that are buckled to it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class IgniteOnBuckleComponent : Component
{
    /// <summary>
    /// How many fire stacks to add per cycle.
    /// </summary>
    [DataField]
    public float FireStacks = 0.5f;

    /// <summary>
    /// How frequently the ignition should be applied, in seconds.
    /// </summary>
    [DataField]
    public float IgniteTime = 1f;

    /// <summary>
    /// Maximum fire stacks that can be added by this source.
    /// If target already has this many or more fire stacks, no additional stacks will be added.
    /// Null means unlimited.
    /// </summary>
    [DataField]
    public float? MaxFireStacks = 2.5f;
}
