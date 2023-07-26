using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Electronics;

[NetworkedComponent()]
[Virtual]
public class SharedDoorElectronicsComponent : Component
{
}

[Serializable, NetSerializable]
public sealed class DoorElectronicsRefreshUiMessage : BoundUserInterfaceMessage
{

}

[Serializable, NetSerializable]
public sealed class DoorElectronicsUpdateConfigurationMessage : BoundUserInterfaceMessage
{
    public List<string> accessList;

    public DoorElectronicsUpdateConfigurationMessage(List<string> _accessList)
    {
        accessList = _accessList;
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
