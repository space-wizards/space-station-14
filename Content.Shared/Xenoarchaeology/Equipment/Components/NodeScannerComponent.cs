using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Xenoarchaeology.Equipment.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), Access(typeof(SharedNodeScannerSystem))]
public sealed partial class NodeScannerComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<string> TriggeredNodesSnapshot = new();
}

[Serializable, NetSerializable]
public enum NodeScannerUiKey : byte
{
    Key
}
