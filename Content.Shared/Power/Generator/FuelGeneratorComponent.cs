using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generator;

/// <summary>
/// This is used for generators that run off some kind of fuel.
/// </summary>
[RegisterComponent]
public sealed class FuelGeneratorComponent : Component
{
    /// <summary>
    /// The amount of fuel left in the generator.
    /// </summary>
    [DataField("remainingFuel"), ViewVariables(VVAccess.ReadWrite)]
    public float RemainingFuel;
    /// <summary>
    /// The generator's target power.
    /// </summary>
    [DataField("targetPower"), ViewVariables(VVAccess.ReadWrite)]
    public float TargetPower = 15_000.0f;
    /// <summary>
    /// The maximum target power.
    /// </summary>
    [DataField("maxTargetPower"), ViewVariables(VVAccess.ReadWrite)]
    public float MaxTargetPower = 30_000.0f;
    /// <summary>
    /// The "optimal" power at which the generator is considered to be at 100% efficiency.
    /// </summary>
    [DataField("optimalPower"), ViewVariables(VVAccess.ReadWrite)]
    public float OptimalPower = 15_000.0f;
    /// <summary>
    /// The rate at which one unit of fuel should be consumed.
    /// </summary>
    [DataField("optimalBurnRate"), ViewVariables(VVAccess.ReadWrite)]
    public float OptimalBurnRate = 1 / 60.0f; // Once every 60 seconds.
}

/// <summary>
/// Sent to the server to adjust the targeted power level.
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
/// Contains network state for FuelGeneratorComponent.
/// </summary>
[Serializable, NetSerializable]
public sealed class SolidFuelGeneratorComponentBuiState : BoundUserInterfaceState
{
    public float RemainingFuel;
    public float TargetPower;
    public float MaximumPower;
    public float OptimalPower;
    public float OptimalBurnRate; // Once every 120 seconds.

    public SolidFuelGeneratorComponentBuiState(FuelGeneratorComponent component)
    {
        RemainingFuel = component.RemainingFuel;
        TargetPower = component.TargetPower;
        MaximumPower = component.MaxTargetPower;
        OptimalPower = component.OptimalPower;
        OptimalBurnRate = component.OptimalBurnRate;
    }
}

[Serializable, NetSerializable]
public enum GeneratorComponentUiKey
{
    Key
}
