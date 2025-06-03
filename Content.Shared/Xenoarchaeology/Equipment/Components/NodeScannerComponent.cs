using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

/// <summary>
/// Component for managing data stored on NodeScanner hand-held device.
/// Can store snapshot list of currently triggered artifact nodes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedNodeScannerSystem))]
public sealed partial class NodeScannerComponent : Component
{
    /// <summary>
    /// Identity-names (3-digit codes) of nodes that are triggered on scanned artifact.
    /// </summary>
    [DataField, AutoNetworkedField]
    public HashSet<string> TriggeredNodesSnapshot = new();

    /// <summary>
    /// State of artifact on the moment of scanning.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ArtifactState ArtifactState;

    /// <summary>
    /// Time until next unlocking of scanned artifact can be started.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? WaitTime;

    /// <summary>
    /// Moment of gametime, at which last artifact scanning was done.
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan? ScannedAt;
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
