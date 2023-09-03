using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Electronics;

[RegisterComponent, NetworkedComponent()]
public sealed partial class DoorElectronicsComponent : Component
{
}

[Serializable, NetSerializable]
public sealed class DoorElectronicsUpdateConfigurationMessage : BoundUserInterfaceMessage
{
    public List<string> accessList;
    public EntityUid actor;

    public DoorElectronicsUpdateConfigurationMessage(List<string> _accessList, EntityUid _actor)
    {
        accessList = _accessList;
        actor = _actor;
    }
}

[Serializable, NetSerializable]
public sealed class DoorElectronicsConfigurationState : BoundUserInterfaceState
{
    public List<string> accessList;

    public DoorElectronicsConfigurationState(List<string> _accessList)
    {
        accessList = _accessList;
    }
}

[Serializable, NetSerializable]
public enum DoorElectronicsConfigurationUiKey : byte
{
    Key
}
