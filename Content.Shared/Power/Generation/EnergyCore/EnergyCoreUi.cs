using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generation.EnergyCore;

[Serializable, NetSerializable]
public enum EnergyCoreState : byte
{
    Disabled,
    Enabled,
    Starting,
    Stopping,
}

[Serializable, NetSerializable]
public enum EnergyCoreVisuals : byte
{
    State,
}

[Serializable, NetSerializable]
public enum EnergyCoreUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class EnergyCoreSetEnabledMessage(bool enabled) : BoundUserInterfaceMessage
{
    public readonly bool Enabled = enabled;
}

[Serializable, NetSerializable]
public sealed class EnergyCoreSetOutputMessage(float output) : BoundUserInterfaceMessage
{
    public readonly float Output = output;
}

[Serializable, NetSerializable]
public sealed class EnergyCoreUiState(
    bool connected,
    EnergyCoreState state = EnergyCoreState.Disabled,
    float fuelEnergy = 0,
    float temperature = 0,
    float restartTemperature = 0,
    float supply = 0,
    float targetSupply = 0,
    float maxSupply = 0,
    float heatOutput = 0,
    float coolantFlow = 0,
    float inletTemperature = 0,
    float outletTemperature = 0,
    float inletPressure = 0,
    float outletPressure = 0,
    float radiation = 0,
    float fissionPower = 0,
    float decayHeat = 0,
    float efficiency = 0,
    float electricalEnergy = 0) : BoundUserInterfaceState
{
    public readonly float FissionPower = fissionPower;
    public readonly float DecayHeat = decayHeat;
    public readonly float Efficiency = efficiency;
    public readonly float ElectricalEnergy = electricalEnergy;
    public readonly bool Connected = connected;
    public readonly float Radiation = radiation;
    public readonly EnergyCoreState State = state;
    public readonly float FuelEnergy = fuelEnergy;
    public readonly float Temperature = temperature;
    public readonly float RestartTemperature = restartTemperature;
    public readonly float Supply = supply;
    public readonly float TargetSupply = targetSupply;
    public readonly float MaxSupply = maxSupply;
    public readonly float HeatOutput = heatOutput;
    public readonly float CoolantFlow = coolantFlow;
    public readonly float InletTemperature = inletTemperature;
    public readonly float OutletTemperature = outletTemperature;
    public readonly float InletPressure = inletPressure;
    public readonly float OutletPressure = outletPressure;
}
