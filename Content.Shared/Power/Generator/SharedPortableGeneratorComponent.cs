using Robust.Shared.Audio;
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

    /// <summary>
    /// Chance that this generator will start. If it fails, the user has to try again.
    /// </summary>
    [DataField("startChance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float StartChance { get; set; } = 0.5f;

    /// <summary>
    /// Amount of time it takes to attempt to start the generator.
    /// </summary>
    [DataField("startTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StartTime { get; set; } = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Sound that plays when attempting to start this generator.
    /// </summary>
    [DataField("startSound")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? StartSound { get; set; }

    /// <summary>
    /// Sound that plays when attempting to start this generator.
    /// Plays instead of <see cref="StartSound"/> if the generator has no fuel (dumbass).
    /// </summary>
    [DataField("startSoundEmpty")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier? StartSoundEmpty { get; set; }
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
/// Sent to the server to set the power switch of a portable generator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PortableGeneratorSetPowerSwitchMessage : BoundUserInterfaceMessage
{
    public bool PowerSwitch;

    public PortableGeneratorSetPowerSwitchMessage(bool powerSwitch)
    {
        PowerSwitch = powerSwitch;
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
    public bool On;

    public PortableGeneratorComponentBuiState(FuelGeneratorComponent component)
    {
        RemainingFuel = component.RemainingFuel;
        TargetPower = component.TargetPower;
        MaximumPower = component.MaxTargetPower;
        OptimalPower = component.OptimalPower;
        On = component.On;
    }
}

[Serializable, NetSerializable]
public enum GeneratorComponentUiKey
{
    Key
}

/// <summary>
/// Sprite layers for generator prototypes.
/// </summary>
[Serializable, NetSerializable]
public enum GeneratorVisualLayers : byte
{
    Body,
}

/// <summary>
/// Appearance keys for generators.
/// </summary>
[Serializable, NetSerializable]
public enum GeneratorVisuals : byte
{
    /// <summary>
    /// Boolean: is the generator running?
    /// </summary>
    Running,
}
