using Robust.Shared.Audio;
using Robust.Shared.Serialization;

namespace Content.Shared.Power.Generator;

/// <summary>
/// Responsible for power output switching &amp; UI logic on portable generators.
/// </summary>
/// <remarks>
/// A portable generator is expected to have the following components: <c>SolidFuelGeneratorAdapterComponent</c> <see cref="FuelGeneratorComponent"/>.
/// </remarks>
/// <seealso cref="SharedPortableGeneratorSystem"/>
[RegisterComponent]
[Access(typeof(SharedPortableGeneratorSystem))]
public sealed partial class PortableGeneratorComponent : Component
{
    /// <summary>
    /// Chance that this generator will start. If it fails, the user has to try again.
    /// </summary>
    [DataField("startChance")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float StartChance { get; set; } = 1f;

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
/// Sent to the server to try to start a portable generator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PortableGeneratorStartMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Sent to the server to try to stop a portable generator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PortableGeneratorStopMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Sent to the server to try to change the power output of a power-switchable portable generator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PortableGeneratorSwitchOutputMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Sent to the server to try to eject all fuel stored in a portable generator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PortableGeneratorEjectFuelMessage : BoundUserInterfaceMessage
{
}

/// <summary>
/// Contains network state for the portable generator.
/// </summary>
[Serializable, NetSerializable]
public sealed class PortableGeneratorComponentBuiState : BoundUserInterfaceState
{
    public float RemainingFuel;
    public bool Clogged;
    public float TargetPower;
    public float MaximumPower;
    public float OptimalPower;
    public bool On;

    public PortableGeneratorComponentBuiState(FuelGeneratorComponent component, float remainingFuel, bool clogged)
    {
        RemainingFuel = remainingFuel;
        Clogged = clogged;
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
    Unlit
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
