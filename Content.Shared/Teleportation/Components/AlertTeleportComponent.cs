using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared.Teleportation.Components;

/// <summary>
/// Allows you to receive alerts with the ability to teleport to a specific entity.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class AlertTeleportComponent : Component
{
    /// <summary>
    /// Stores information about targets and accepts the
    /// AlertPrototype as a key for sorting targets by alerts
    /// </summary>
    [AutoNetworkedField, ViewVariables]
    public Dictionary<ProtoId<AlertPrototype>, AlertTeleportData> Targets = new();

    /// <summary>
    /// Should it just teleport or orbit?
    /// </summary>
    [DataField]
    public bool Orbit = true;
}

/// <summary>
/// It stores information about entities to which you can teleport,
/// a place in the queue to move through different entities when you click again,
/// and the time after which you need to reset the list of available entities.
/// </summary>
[Serializable, NetSerializable]
public struct AlertTeleportData
{
    // Not an EntityUid for fine serializable
    public List<NetEntity> Targets = [];
    public int Queue;
    public TimeSpan EndTime;

    public AlertTeleportData()
    {
    }
}

