using Content.Shared.Atmos.Monitor;
using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components;

[Serializable, NetSerializable]
public sealed partial class GasThermoMachineDataPayload : AtmosDeviceDataPayload
{
    [DataField]
    public float EnergyDelta;
}

[Serializable, NetSerializable]
public sealed partial class GasThermoMachineSyncDataPayload : HandledNetworkPayload;

[Serializable, NetSerializable]
public sealed partial class GasThermoMachineSetDataPayload : HandledNetworkPayload
{
    [DataField]
    public GasThermoMachineDataPayload Payload;
}

[Serializable]
[NetSerializable]
public enum ThermomachineUiKey : byte
{
    Key
}

[Serializable]
[NetSerializable]
public sealed class GasThermomachineToggleMessage : BoundUserInterfaceMessage
{
}

[Serializable]
[NetSerializable]
public sealed class GasThermomachineChangeTemperatureMessage : BoundUserInterfaceMessage
{
    public float Temperature { get; }

    public GasThermomachineChangeTemperatureMessage(float temperature)
    {
        Temperature = temperature;
    }
}
