using Robust.Shared.Serialization;

namespace Content.Shared.OuterRim.Generator;

/// <summary>
/// This is used for...
/// </summary>
[RegisterComponent]
public sealed class SharedGeneratorComponent : Component
{
    [DataField("remainingFuel"), ViewVariables(VVAccess.ReadWrite)]
    public float RemainingFuel = 0.0f;

    [DataField("targetPower"), ViewVariables(VVAccess.ReadWrite)]
    public float TargetPower = 15_000.0f;
    [DataField("optimalPower"), ViewVariables(VVAccess.ReadWrite)]
    public float OptimalPower = 15_000.0f;
    [DataField("optimalBurnRate"), ViewVariables(VVAccess.ReadWrite)]
    public float OptimalBurnRate = 1 / 4.0f; // Once every 45 seconds.

    [DataField("fuelMaterial"), ViewVariables(VVAccess.ReadWrite)]
    public string FuelMaterial = "Plasma";
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
/// Contains network state for SharedGeneratorComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class GeneratorComponentBuiState : BoundUserInterfaceState
{
    public float RemainingFuel;
    public float TargetPower;
    public float OptimalPower;
    public float OptimalBurnRate; // Once every 120 seconds.

    public GeneratorComponentBuiState(SharedGeneratorComponent component)
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
