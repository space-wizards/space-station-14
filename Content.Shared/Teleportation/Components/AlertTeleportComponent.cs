using Content.Shared.Alert;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlertTeleportComponent : Component
{
    [AutoNetworkedField, ViewVariables]
    public Dictionary<ProtoId<AlertPrototype>, AlertTeleportData> Targets = new();

    [DataField]
    public bool Orbit = true;
}

[Serializable, NetSerializable]
public struct AlertTeleportData
{
    // Not an EntityUid for fine serializable
    public List<NetEntity> Targets;
    public int Queue;
    public TimeSpan EndTime;
}

