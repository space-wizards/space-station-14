using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Sent to the server to adjust the targeted power level of a portable generator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PortableGeneratorSetTargetPowerMessage : BoundUserInterfaceMessage
{
    public int TargetPower;

    public PortableGeneratorSetTargetPowerMessage(int targetPower)
    {
        TargetPower = targetPower;
    }
}

/// <summary>
/// Contains network state for the portable generator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PortableGeneratorComponentBuiState : BoundUserInterfaceState
{
    public float RemainingFuel;
    public float TargetPower;
    public float MaximumPower;
    public float OptimalPower;

    public PortableGeneratorComponentBuiState(FuelGeneratorComponent component)
    {
        RemainingFuel = component.RemainingFuel;
        TargetPower = component.TargetPower;
        MaximumPower = component.MaxTargetPower;
        OptimalPower = component.OptimalPower;
    }
}

[Serializable, NetSerializable]
public enum GeneratorComponentUiKey
{
    Key
}
