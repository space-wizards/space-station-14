using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// Component for NodeScanner hand-held device settings.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(NodeScannerSystem))]
public sealed partial class NodeScannerComponent : Component
{
    /// <summary>
    /// Maximum range for keeping connection to artifact.
    /// </summary>
    [DataField]
    public int MaxLinkedRange = 5;

    /// <summary>
    /// Update interval for link info.
    /// </summary>
    [DataField]
    public TimeSpan DisplayDataUpdateInterval = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Component-marker that node scanner device (<see cref="NodeScannerComponent"/>) is connected to artifact.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
public sealed partial class NodeScannerConnectedComponent : Component
{
    /// <summary>
    /// Xeno artifact entity, to which scanner is attached currently.
    /// Upon detaching this component should be removed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid AttachedTo;

    /// <summary>
    /// Next update tick gametime.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    /// <summary>
    /// Update interval for link info.
    /// </summary>
    [DataField]
    public TimeSpan LinkUpdateInterval = TimeSpan.FromSeconds(1);
}

/// <summary>
/// Displayable to player artifact states.
/// </summary>
[Serializable, NetSerializable]
public enum ArtifactState
{
    /// <summary> Unused default. </summary>
    None,
    /// <summary> Artifact is ready to start unlocking. </summary>
    Ready,
    /// <summary> Artifact is in unlocking state, listening to any additional trigger. </summary>
    Unlocking,
    /// <summary> Artifact unlocking is on cooldown, nodes could not be triggered. </summary>
    Cooldown
}

[Serializable, NetSerializable]
public enum NodeScannerUiKey : byte
{
    Key
}
