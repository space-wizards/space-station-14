using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedNodeScannerSystem))]
public sealed partial class NodeScannerComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<string> TriggeredNodesSnapshot = new();

    [DataField, AutoNetworkedField]
    public ArtifactState ArtifactState;

    [DataField, AutoNetworkedField]
    public TimeSpan? WaitTime;

    [DataField, AutoNetworkedField]
    public TimeSpan? ScannedAt { get; set; }
}

[Serializable, NetSerializable]
public enum ArtifactState
{
    None,
    Ready,
    Unlocking,
    Cooldown
}

[Serializable, NetSerializable]
public enum NodeScannerUiKey : byte
{
    Key
}
