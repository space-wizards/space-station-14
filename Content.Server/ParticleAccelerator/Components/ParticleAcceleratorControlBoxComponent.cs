using Content.Server.ParticleAccelerator.Wires;
using Content.Shared.Singularity.Components;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.ParticleAccelerator.Components;

// This component is in control of the PA's logic because it's the one to contain the wires for hacking.
// And also it's the only PA component that meaningfully needs to work on its own.
/// <summary>
///     Is the computer thing people interact with to control the PA.
///     Also contains primary logic for actual PA behavior, part scanning, etc...
/// </summary>
[RegisterComponent]
public sealed partial class ParticleAcceleratorControlBoxComponent : Component
{
    /// <summary>
    /// Whether the PA parts have been correctly arranged to make a functional device.
    /// </summary>
    [ViewVariables]
    public bool Assembled = false;

    /// <summary>
    /// Whether the PA is currently set to fire at the console.
    /// Requires <see cref="Assembled"/> to be true.
    /// </summary>
    [ViewVariables]
    public bool Enabled = false;

    /// <summary>
    /// Whether the PA actually has the power necessary to fire.
    /// Requires <see cref="Enabled"/> to be true.
    /// </summary>
    [ViewVariables]
    public bool Powered = false;

    /// <summary>
    /// Whether the PA is currently firing or charging to fire.
    /// Requires <see cref="Powered"/> to be true.
    /// </summary>
    [ViewVariables]
    public bool Firing = false;

    /// <summary>
    /// Block re-entrant rescanning.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool CurrentlyRescanning = false;

    /// <summary>
    /// Whether the PA is currently firing or charging to fire.
    /// Bounded by <see cref="ParticleAcceleratorPowerState.Standby"/> and <see cref="MaxStrength"/>.
    /// Modified by <see cref="ParticleAcceleratorStrengthWireAction"/>.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public ParticleAcceleratorPowerState SelectedStrength = ParticleAcceleratorPowerState.Standby;

    /// <summary>
    /// The maximum strength level this particle accelerator can be set to operate at.
    /// Modified by <see cref="ParticleAcceleratorLimiterWireAction"/>.
    /// </summary>
    [ViewVariables]
    public ParticleAcceleratorPowerState MaxStrength = ParticleAcceleratorPowerState.Level2;

    /// <summary>
    /// The power supply unit of the assembled particle accelerator.
    /// Implies the existance of a <see cref="ParticleAcceleratorPowerBoxComponent"/> attached to this entity.
    /// </summary>
    [ViewVariables]
    public EntityUid? PowerBox;

    /// <summary>
    /// Whether the PA is currently firing or charging to fire.
    /// Implies the existance of a <see cref="ParticleAcceleratorEndCapComponent"/> attached to this entity.
    /// </summary>
    [ViewVariables]
    public EntityUid? EndCap;

    /// <summary>
    /// Whether the PA is currently firing or charging to fire.
    /// Implies the existance of a <see cref="ParticleAcceleratorFuelChamberComponent"/> attached to this entity.
    /// </summary>
    [ViewVariables]
    public EntityUid? FuelChamber;

    /// <summary>
    /// Whether the PA is currently firing or charging to fire.
    /// Implies the existance of a <see cref="ParticleAcceleratorEmitterComponent"/> attached to this entity.
    /// </summary>
    [ViewVariables]
    public EntityUid? PortEmitter;

    /// <summary>
    /// Whether the PA is currently firing or charging to fire.
    /// Implies the existance of a <see cref="ParticleAcceleratorEmitterComponent"/> attached to this entity.
    /// </summary>
    [ViewVariables]
    public EntityUid? ForeEmitter;

    /// <summary>
    /// Whether the PA is currently firing or charging to fire.
    /// Implies the existance of a <see cref="ParticleAcceleratorEmitterComponent"/> attached to this entity.
    /// </summary>
    [ViewVariables]
    public EntityUid? StarboardEmitter;

    /// <summary>
    /// The amount of power the particle accelerator must be provided with relative to the expected power draw to function.
    /// </summary>
    [ViewVariables]
    public const float RequiredPowerRatio = 0.999f;

    /// <summary>
    /// The amount of power (in watts) the PA draws just by existing as a functional machine.
    /// </summary>
    [DataField("powerDrawBase")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int BasePowerDraw = 500;

    /// <summary>
    /// The amount of power (in watts) the PA draws per level when turned on.
    /// </summary>
    [DataField("powerDrawMult")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int LevelPowerDraw = 1500;

    /// <summary>
    /// The time at which the PA last fired a wave of particles.
    /// </summary>
    [DataField("lastFire")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan LastFire;

    /// <summary>
    /// The time at which the PA will next fire a wave of particles.
    /// </summary>
    [DataField("nextFire")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan NextFire;

    /// <summary>
    /// Delay between consecutive PA shots.
    /// </summary>
    // Fun fact:
    // On /vg/station (can't check TG because lol they removed singulo),
    // the PA emitter parts have var/fire_delay = 50.
    // For anybody from the future not BYOND-initiated, that's 5 seconds.
    // However, /obj/machinery/particle_accelerator/control_box/process(),
    // which calls emit_particle() on the emitters,
    // only gets called every *2* seconds, because of CarnMC timing.
    // So the *actual* effective firing delay of the PA is 6 seconds, not 5 as listed in the code.
    // So...
    // I have reflected that here to be authentic.
    [DataField("chargeTime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan ChargeTime = TimeSpan.FromSeconds(6.0);

    /// <summary>
    /// Whether the interface has been disabled via a cut wire or not.
    /// Modified by <see cref="ParticleAcceleratorKeyboardWireAction"/>.
    /// </summary>
    [ViewVariables]
    public bool InterfaceDisabled = false;

    /// <summary>
    /// Whether the ability to change the strength of the PA has been disabled via a cut wire or not.
    /// Modified by <see cref="ParticleAcceleratorStrengthWireAction"/>.
    /// </summary>
    [ViewVariables]
    public bool StrengthLocked = false;

    /// <summary>
    /// Time at which the admin alarm sound effect can next be played.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan EffectCooldown;

    /// <summary>
    /// Time between admin alarm sound effects. Prevents spam
    /// </summary>
    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(10f);

    /// <summary>
    /// Whether the PA can be turned on.
    /// Modified by <see cref="ParticleAcceleratorPowerWireAction"/>.
    /// </summary>
    [ViewVariables]
    public bool CanBeEnabled = true;
}
