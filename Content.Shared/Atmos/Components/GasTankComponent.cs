using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasTankComponent : Component, IGasMixtureHolder
{
    public const float MaxExplosionRange = 26f;
    private const float DefaultLowPressure = 0f;
    private const float DefaultOutputPressure = Atmospherics.OneAtmosphere;

    public int Integrity = 3;
    public bool IsLowPressure => Air.Pressure <= TankLowPressure;

    [DataField]
    public SoundSpecifier RuptureSound = new SoundPathSpecifier("/Audio/Effects/spray.ogg");

    [DataField]
    public SoundSpecifier? ConnectSound =
        new SoundPathSpecifier("/Audio/Effects/internals.ogg")
        {
            Params = AudioParams.Default.WithVolume(5f),
        };

    [DataField]
    public SoundSpecifier? DisconnectSound;

    // Cancel toggles sounds if we re-toggle again.

    public EntityUid? ConnectStream;
    public EntityUid? DisconnectStream;

    [DataField]
    public GasMixture Air { get; set; } = new();

    /// <summary>
    ///     Pressure at which tank should be considered 'low' such as for internals.
    /// </summary>
    [DataField]
    public float TankLowPressure = DefaultLowPressure;

    /// <summary>
    ///     Distributed pressure.
    /// </summary>
    [DataField]
    public float OutputPressure = DefaultOutputPressure;

    /// <summary>
    ///     The maximum allowed output pressure.
    /// </summary>
    [DataField]
    public float MaxOutputPressure = 3 * DefaultOutputPressure;

    /// <summary>
    ///     Tank is connected to internals.
    /// </summary>
    [ViewVariables]
    public bool IsConnected => User != null;

    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    ///     True if this entity was recently moved out of a container. This might have been a hand -> inventory
    ///     transfer, or it might have been the user dropping the tank. This indicates the tank needs to be checked.
    /// </summary>
    [ViewVariables]
    public bool CheckUser;

    /// <summary>
    ///     Pressure at which tanks start leaking.
    /// </summary>
    [DataField]
    public float TankLeakPressure = 30 * Atmospherics.OneAtmosphere;

    /// <summary>
    ///     Pressure at which tank spills all contents into atmosphere.
    /// </summary>
    [DataField]
    public float TankRupturePressure = 40 * Atmospherics.OneAtmosphere;

    /// <summary>
    ///     Base 3x3 explosion.
    /// </summary>
    [DataField]
    public float TankFragmentPressure = 50 * Atmospherics.OneAtmosphere;

    /// <summary>
    ///     Increases explosion for each scale kPa above threshold.
    /// </summary>
    [DataField]
    public float TankFragmentScale = 2.25f * Atmospherics.OneAtmosphere;

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleInternals";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;

    /// <summary>
    ///     Valve to release gas from tank
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool IsValveOpen;

    /// <summary>
    ///     Gas release rate in L/s
    /// </summary>
    [DataField, AutoNetworkedField]
    public float ValveOutputRate = 100f;

    [DataField]
    public SoundSpecifier ValveSound =
        new SoundCollectionSpecifier("valveSqueak")
        {
            Params = AudioParams.Default.WithVolume(-5f),
        };
}
