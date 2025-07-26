using Content.Shared.Atmos;
using Content.Shared.Explosion.EntitySystems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Explosion.Components.OnTrigger;

/// <summary>
/// Contains a GasMixture that will release its contents to the atmosphere when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
[Access(typeof(SharedReleaseGasOnTriggerSystem))]
public sealed partial class ReleaseGasOnTriggerComponent : Component
{
    /// <summary>
    /// Whether this grenade is active and releasing gas.
    /// Set to true when triggered, which starts gas release.
    /// </summary>
    [DataField]
    public bool Active;

    /// <summary>
    /// The gas mixture that will be released to the current tile atmosphere when triggered.
    /// </summary>
    [DataField]
    public GasMixture Air;

    /// <summary>
    /// Time at which the next release will occur.
    /// This is automatically set when the grenade activates.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextReleaseTime = TimeSpan.Zero;

    /// <summary>
    /// The cap at which this grenade can fill the exposed atmosphere to.
    /// This component automatically removes itself when the pressure limit is reached.
    /// </summary>
    /// <example>If set to 101.325, the grenade will only fill the exposed
    /// atmosphere up to 101.325 kPa.</example>
    /// <remarks>If zero, this limit won't be respected.</remarks>
    [DataField]
    public float PressureLimit;

    /// <summary>
    /// How often the grenade will release gas.
    /// </summary>
    [DataField]
    public TimeSpan ReleaseInterval = TimeSpan.FromSeconds(1);

    /// <summary>
    /// A float from 0 to 1, representing a partial portion of the moles
    /// of the gas mixture that will be
    /// released to the current tile atmosphere when triggered.
    /// </summary>
    /// <remarks>If undefined on the prototype, the entire molar amount will be transferred.</remarks>
    [DataField]
    public float RemoveFraction = 1;

    /// <summary>
    /// Stores the total moles initially in the grenade upon activation.
    /// Used to calculate the moles released over time.
    /// </summary>
    /// <remarks>Set when the grenade is activated.</remarks>
    [DataField(readOnly: true)]
    public float StartingTotalMoles;
}

/// <summary>
/// Represents visual states for whatever visuals that need to be applied
/// on state changes.
/// </summary>
[Serializable, NetSerializable]
public enum ReleaseGasOnTriggerVisuals : byte
{
    Key,
}
