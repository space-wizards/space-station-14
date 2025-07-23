using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._CorvaxNext.Silicons.Borgs.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class AiRemoteControllerComponent : Component
{
    [DataField] public EntityUid? AiHolder;
    [DataField] public EntityUid? LinkedMind;

    [DataField] public string[]? PreviouslyTransmitterChannels;
    [DataField] public string[]? PreviouslyActiveRadioChannels;

    [DataField] public EntProtoId BackToAiAction = "ActionBackToAi";
    [DataField] public EntityUid? BackToAiActionEntity;

    [Serializable, NetSerializable]
    public sealed class RemoteDeviceActionMessage : BoundUserInterfaceMessage
    {
        public readonly RemoteDeviceActionEvent? RemoteAction;
        public RemoteDeviceActionMessage(RemoteDeviceActionEvent remoteDeviceAction)
        {
            RemoteAction = remoteDeviceAction;
        }
    }
}

[Serializable, NetSerializable]
public sealed class RemoteDeviceActionEvent : EntityEventArgs
{
    public enum RemoteDeviceActionType
    {
        MoveToDevice,
        TakeControl
    }
    public RemoteDeviceActionType ActionType;
    public NetEntity Target;

    public RemoteDeviceActionEvent(RemoteDeviceActionType actionType, NetEntity target)
    {
        ActionType = actionType;
        Target = target;
    }
}

[Serializable, NetSerializable]
public record struct RemoteDevicesData()
{
    public string DisplayName = string.Empty;

    public NetEntity NetEntityUid = NetEntity.Invalid;
}

[Serializable, NetSerializable]
public sealed class RemoteDevicesBuiState : BoundUserInterfaceState
{
    public List<RemoteDevicesData> DeviceList;

    public RemoteDevicesBuiState(List<RemoteDevicesData> deviceList)
    {
        DeviceList = deviceList;
    }
}
