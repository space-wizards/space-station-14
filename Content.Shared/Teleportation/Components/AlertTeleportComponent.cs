using Content.Shared.Alert;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;


namespace Content.Shared.Teleportation.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(AlertTeleportSystem))]
public sealed partial class AlertTeleportComponent : Component
{
    [ViewVariables, AutoNetworkedField]
    public Dictionary<ProtoId<AlertPrototype>, AlertTeleportData> Targets = new();

    [DataField]
    public bool Orbit = true;
}

[Serializable, NetSerializable]
public struct AlertTeleportData
{
    public List<NetEntity> Targets;
    public int Queue;
    public TimeSpan EndTime;
}

