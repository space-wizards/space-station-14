using Content.Shared.DeviceNetwork;
using Robust.Shared.Serialization;

namespace Content.Shared.Atmos.Piping.Unary.Components;

public sealed partial class GasThermoMachineDataPayload : NetworkPayloadBase<GasThermoMachineDataPayload>
{
    [DataField]
    public float EnergyDelta;
}

public sealed partial class GasThermoMachineSyncDataPayload : NetworkPayloadBase<GasThermoMachineSyncDataPayload>;

public sealed partial class GasThermoMachineSetDataPayload : NetworkPayloadBase<GasThermoMachineSetDataPayload>
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
