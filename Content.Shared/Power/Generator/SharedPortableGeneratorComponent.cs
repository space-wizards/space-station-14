using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Responsible for power output switching &amp; UI logic on portable generators.
/// </summary>
/// <remarks>
/// A portable generator is expected to have the following components: <c>SolidFuelGeneratorAdapterComponent</c> <see cref="FuelGeneratorComponent"/>.
/// </remarks>
/// <seealso cref="SharedPortableGeneratorSystem"/>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedPortableGeneratorSystem))]
public sealed partial class PortableGeneratorComponent : Component
{
    /// <summary>
    /// Which output the portable generator is currently connected to.
    /// </summary>
    [DataField("activeOutput")]
    [AutoNetworkedField]
    public PortableGeneratorPowerOutput ActiveOutput { get; set; }
}

/// <summary>
/// Possible power output for portable generators.
/// </summary>
/// <seealso cref="PortableGeneratorComponent"/>
[Serializable, NetSerializable]
public enum PortableGeneratorPowerOutput : byte
{
    /// <summary>
    /// The generator is set to connect to a high-voltage power network.
    /// </summary>
    HV,

    /// <summary>
    /// The generator is set to connect to a medium-voltage power network.
    /// </summary>
    MV
}

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
