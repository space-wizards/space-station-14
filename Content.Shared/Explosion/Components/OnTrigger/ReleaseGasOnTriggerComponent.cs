using Content.Shared.Atmos;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Explosion.Components.OnTrigger;

/// <summary>
/// Contains a GasMixture that will release its contents to the atmosphere when triggered.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentPause]
public sealed partial class ReleaseGasOnTriggerComponent : Component, IGasMixtureHolder
{
    /// <summary>
    /// Whether this grenade is active and releasing gas.
    /// Set to true when triggered, which starts gas release.
    /// </summary>
    [DataField]
    public bool Active;

    /// <summary>
    /// Limit the flow rate of the gas released to the atmosphere, in L/s.
    /// </summary>
    /// <remarks>If zero, the flow rate will be unlimited.</remarks>
    [DataField]
    public float FlowRateLimit = 200;

    /// <summary>
    /// Time at which the next release will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    [AutoPausedField]
    public TimeSpan NextReleaseTime = TimeSpan.Zero;

    /// <summary>
    /// The cap at which this grenade can fill the exposed atmosphere to.
    /// The grenade automatically deletes itself when the pressure is reached.
    /// </summary>
    /// <example>If set to 101.325, the grenade will only fill the exposed
    /// atmosphere up to 101.325 kPa.</example>
    /// <remarks>If zero, this limit won't be respected.</remarks>
    [DataField]
    public float PressureLimit;

    /// <summary>
    /// How often the grenade will update.
    /// </summary>
    [DataField]
    public TimeSpan ReleaseInterval = TimeSpan.FromSeconds(0.5f);

    /// <summary>
    /// Determines the length of time the gas will be released over, in seconds.
    /// </summary>
    /// <example>If set to 5, the grenade will partition the gas
    /// to release the full amount over the course of 5 seconds.</example>
    /// <remarks>If zero (unset), the gas will be released instantly unless restricted
    /// in some other way (ex. <see cref="FlowRateLimit"/>)</remarks>
    [DataField]
    public float ReleaseOverTimespan = 5;

    /// <summary>
    /// A partial portion of the volume of the gas mixture that will be
    /// released to the current tile atmosphere when triggered, used when
    /// <see cref="ReleaseOverTimespan"/> is populated.
    /// </summary>
    [DataField(readOnly: true)]
    public float VolumeFraction;

    /// <summary>
    /// The gas mixture that will be released to the current tile atmosphere when triggered.
    /// </summary>
    [DataField]
    public GasMixture Air { get; set; }
}
