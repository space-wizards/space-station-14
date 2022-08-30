using Robust.Shared.Serialization;

namespace Content.Shared.OuterRim.Generator;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class SharedSolidFuelGeneratorComponent : Component
{
    [DataField("remainingFuel"), ViewVariables(VVAccess.ReadWrite)]
    public float RemainingFuel = 0.0f;

    [DataField("targetPower"), ViewVariables(VVAccess.ReadWrite)]
    public float TargetPower = 1_500.0f;
    [DataField("optimalPower"), ViewVariables(VVAccess.ReadWrite)]
    public float OptimalPower = 1_500.0f;
    [DataField("optimalBurnRate"), ViewVariables(VVAccess.ReadWrite)]
    public float OptimalBurnRate = 1 / 60.0f; // Once every 60 seconds.
}

/// <summary>
/// Sent to the server to adjust the targetted power level.
/// </summary>
[Serializable, NetSerializable]
public sealed class SetTargetPowerMessage : BoundUserInterfaceMessage
{
    public int TargetPower;

    public SetTargetPowerMessage(int targetPower)
    {
        TargetPower = targetPower;
    }
}

/// <summary>
/// Contains network state for SharedSolidFuelGeneratorComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class SolidFuelGeneratorComponentBuiState : BoundUserInterfaceState
{
    public float RemainingFuel;
    public float TargetPower;
    public float OptimalPower;
    public float OptimalBurnRate; // Once every 120 seconds.

    public SolidFuelGeneratorComponentBuiState(SharedSolidFuelGeneratorComponent component)
    {
        RemainingFuel = component.RemainingFuel;
        TargetPower = component.TargetPower;
        OptimalPower = component.OptimalPower;
        OptimalBurnRate = component.OptimalBurnRate;
    }
}

[Serializable, NetSerializable]
public enum GeneratorComponentUiKey
{
    Key
}
