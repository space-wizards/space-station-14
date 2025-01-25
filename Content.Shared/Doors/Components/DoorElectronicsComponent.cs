using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.Access;

namespace Content.Shared.Doors.Components;

/// <summary>
/// Allows an entity's AccessReader to be configured via UI.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class DoorElectronicsComponent : Component;

[Serializable, NetSerializable]
public sealed class DoorElectronicsUpdateConfigurationMessage(List<ProtoId<AccessLevelPrototype>> accessList)
    : BoundUserInterfaceMessage
{
    public List<ProtoId<AccessLevelPrototype>> AccessList = accessList;
}

[Serializable, NetSerializable]
public sealed class DoorElectronicsConfigurationState(List<ProtoId<AccessLevelPrototype>> accessList)
    : BoundUserInterfaceState
{
    public List<ProtoId<AccessLevelPrototype>> AccessList = accessList;
}

[Serializable, NetSerializable]
public enum DoorElectronicsConfigurationUiKey : byte
{
    Key,
}
