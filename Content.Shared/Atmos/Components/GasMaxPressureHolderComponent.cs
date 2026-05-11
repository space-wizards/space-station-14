using Robust.Shared.Audio;

namespace Content.Shared.Atmos.Components;

/// <summary>
/// An atmos device that can hold and release gas. Such as a Gas Tank or Gas Canister.
/// Abstract as both of these devices share a lot of similar behavior.
/// </summary>
public abstract partial class GasMaxPressureHolderComponent : Component, IGasMaxPressureHolder
{
    private const float DefaultIntegrity = 3f;
    private const float DefaultOutputPressure = Atmospherics.OneAtmosphere;

    /// <summary>
    ///     Minimum release pressure possible for the release valve.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MinReleasePressure { get; set; } = DefaultOutputPressure / 10;

    /// <summary>
    ///     Maximum release pressure possible for the release valve.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float MaxReleasePressure { get; set; } = 3 * DefaultOutputPressure;

    /// <summary>
    ///     Current Valve release pressure.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ReleasePressure { get; set; } = DefaultOutputPressure;

    /// <summary>
    ///     Whether the release valve is open on this device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool ReleaseValveOpen { get; set; }

    /// <summary>
    /// Sound made when the valve on this device is opened or closed.
    /// </summary>
    [DataField]
    public SoundSpecifier ValveSound =
        new SoundCollectionSpecifier("valveSqueak")
        {
            Params = AudioParams.Default.WithVolume(-5f),
        };

    /// <summary>
    /// Sound made when air is leaked out of this device.
    /// </summary>
    [DataField]
    public SoundSpecifier? ReleaseSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/spray.ogg")
    {
        Params = AudioParams.Default.WithVolume(-5f),
    };

    /// <summary>
    /// The mixture of air contained in this device.
    /// </summary>
    [DataField, AutoNetworkedField]
    public GasMixture Air { get; set; }

    // TODO ATMOS: Proper loud BANG sound, these are lethal concussive blast waves
    [DataField]
    public SoundSpecifier? RuptureSound { get; set; } = new SoundPathSpecifier("/Audio/Effects/spray.ogg");

    // The values chosen for safety and overpressure are arbitrary, if reactions change feel free to change these to whatever is most fun!
    // Generally a lower overpressure value and lower safety pressure mean weaker bombs with shorter fuses.
    [DataField]
    public float SafetyPressure { get; set; } = 15 * Atmospherics.OneAtmosphere;

    [DataField]
    public float Overpressure { get; set; } = 20 * Atmospherics.OneAtmosphere;

    [DataField]
    public LocId? SafetyAlert { get; set; } = "gas-max-pressure-alert";

    [DataField]
    public float MaxIntegrity { get; set; } = DefaultIntegrity;

    [DataField]
    public float Integrity { get; set; } = DefaultIntegrity;
}
