using Content.Shared.Damage.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Silicons.StationAi;

/// <summary>
/// Attached to entities that can fully heal other entities contained
/// within themselves after a period of time has past.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ReviverDeviceSystem))]
public sealed partial class ReviverDeviceComponent : Component
{
    /// <summary>
    /// The number of discrete stages that the target will progresses through
    /// before being healed.
    /// </summary>
    [DataField]
    public int RestorationStageCount = 4;

    /// <summary>
    /// Determines how much damage can be healed per second by this device.
    /// Note that any and all healing is only applied to a target once
    /// <see cref="RestorationEndTime"/> has been reached.
    /// </summary>
    [DataField]
    public float RestorationRate = 1f;

    /// <summary>
    /// If true, the device will attempt to return the target to life
    /// at the end of the recovery period.
    /// </summary>
    [DataField]
    public bool ResurrectTarget = true;

    /// <summary>
    /// The name of the container in which valid targets for restoration
    /// can be found.
    /// </summary>
    [DataField]
    public string RestorationContainer = string.Empty;

    /// <summary>
    /// If an entity inserted into <see cref="RestorationContainer"/> is not
    /// a valid target for restoration, the inserted entity will be searched
    /// for any containers on this list to see if an valid target can be found.
    /// </summary>
    [DataField]
    public List<string> TargetContainers = new();

    /// <summary>
    /// The time at which the restoration commenced.
    /// </summary>
    [DataField]
    public TimeSpan RestorationStartTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// The time at which the restoration will end and healing will be applied.
    /// </summary>
    [DataField]
    public TimeSpan RestorationEndTime = TimeSpan.FromSeconds(0);

    /// <summary>
    /// Indicates whether there is a restoration in progress.
    /// </summary>
    [DataField]
    public bool RestorationInProgress = false;
}

[Serializable, NetSerializable]
public enum ReviverDeviceVisuals : byte
{
    MobState,
    RestorationProgress,
}

[ByRefEvent]
public record struct InitiateReviverDeviceRestorationEvent;
