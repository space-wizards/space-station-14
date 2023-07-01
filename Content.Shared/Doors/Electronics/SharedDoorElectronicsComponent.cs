using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Electronics
{
    [NetworkedComponent()]
    [Virtual]
    public class SharedDoorElectronicsComponent : Component
    {
        [Serializable, NetSerializable]
        public sealed class RefreshUiMessage : BoundUserInterfaceMessage
        {

        }

        [Serializable, NetSerializable]
        public sealed class UpdateConfigurationMessage : BoundUserInterfaceMessage
        {
            public List<string> accessList;

            public UpdateConfigurationMessage(List<string> _accessList)
            {
                accessList = _accessList;
            }
        }

        [Serializable, NetSerializable]
        public sealed class ConfigurationState : BoundUserInterfaceState
        {
            public List<string> accessList;

            public ConfigurationState(List<string> _accessList)
            {
                accessList = _accessList;
            }
        }
    }

}
