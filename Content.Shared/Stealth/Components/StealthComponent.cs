using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Stealth.Components;
/// <summary>
/// Add this component to an entity that you want to be cloaked.
/// It overlays a shader on the entity to give them an invisibility cloaked effect
/// It also turns the entity invisible
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedStealthSystem))]
public sealed class StealthComponent : Component
{
    /// <summary>
    /// Whether or not the entity previously had an interaction outline prior to cloaking.
    /// </summary>
    [DataField("hadOutline")]
    public bool HadOutline;

    /// <summary>
    /// Last set level of visibility. Ranges from 1 (fully visible) and -1 (fully hidden). To get the actual current
    /// visibility, use <see cref="SharedStealthSystem.GetVisibility(EntityUid, StealthComponent?)"/>
    /// </summary>
    [DataField("lastVisibility")]
    [Access(typeof(SharedStealthSystem),  Other = AccessPermissions.None)]
    public float LastVisibility;

    /// <summary>
    /// Time at which <see cref="LastVisibility"/> was set. Null implies the entity is currently paused and not
    /// accumulating any visibility change.
    /// </summary>
    [DataField("lastUpdate", customTypeSerializer:typeof(TimeOffsetSerializer))]
    public TimeSpan? LastUpdated;

    /// <summary>
    /// Rate that effects how fast an entity's visibility passively changes.
    /// </summary>
    [DataField("passiveVisibilityRate")]
    public readonly float PassiveVisibilityRate = -0.15f;

    /// <summary>
    /// Rate for movement induced visibility changes. Scales with distance moved.
    /// </summary>
    [DataField("movementVisibilityRate")]
    public readonly float MovementVisibilityRate = 0.2f;
}

[Serializable, NetSerializable]
public sealed class StealthComponentState : ComponentState
{
    public float Visibility;
    public TimeSpan? LastUpdated;

    public StealthComponentState(float stealthLevel, TimeSpan? lastUpdated)
    {
        Visibility = stealthLevel;
        LastUpdated = lastUpdated;
    }
}
