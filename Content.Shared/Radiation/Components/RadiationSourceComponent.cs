using Content.Shared.Radiation.Systems;
using Robust.Shared.GameStates;

namespace Content.Shared.Radiation.Components;

/// <summary>
/// Irradiate all objects in range, optionally decaying in intensity over time.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, Access(typeof(SharedRadiationSystem))]
public sealed partial class RadiationSourceComponent : Component
{
    /// <summary>
    ///     Radiation intensity in center of the source in rads per second.
    ///     From there radiation rays will travel over distance and loose intensity
    ///     when hit radiation blocker.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("intensity")]
    public float Intensity = 1;

    /// <summary>
    ///     Defines how fast radiation rays will loose intensity
    ///     over distance. The bigger the value, the shorter range
    ///     of radiation source will be.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("slope")]
    public float Slope = 0.5f;

    /// <summary>
    /// How many seconds it takes for half of the intensity to decay.
    /// If 0 then the source will not decay.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite), DataField("halfLife"), AutoNetworkedField]
    public float HalfLife
    {
        get
        {
            return _halfLife;
        }
        set
        {
            _halfLife = value;
            DecayRate = value == 0
                ? 0f
                : -(MathF.Log(0.5f) / value);
        }
    }

    private float _halfLife;

    /// <summary>
    /// Rate at which the source intensity decreases, the higher the faster.
    /// If 0 then the source will not decay.
    /// Derived from <see cref="HalfLife"/> when it changes.
    /// </summary>
    [ViewVariables]
    public float DecayRate;
}
