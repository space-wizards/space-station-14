using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Buckle.Components;

/// <summary>
/// Component for entities that are currently being ignited by <see cref="IgniteOnBuckleComponent"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause, AutoGenerateComponentState]
public sealed partial class ActiveIgniteOnBuckleComponent : Component
{
    // We cache data in this component and apply it to those who get buckled to have to do less lookups.

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

    /// <summary>
    /// Next time that fire stacks will be applied.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField, AutoNetworkedField]
    public TimeSpan NextIgniteTime = TimeSpan.Zero;
}
