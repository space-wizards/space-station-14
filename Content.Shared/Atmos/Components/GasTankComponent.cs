using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.Atmos.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
public sealed partial class GasTankComponent : GasMaxPressureHolderComponent
{
    private const float DefaultLowPressure = Atmospherics.OneAtmosphere;

    public bool IsLowPressure => Air.Pressure <= TankLowPressure;

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

    /// <summary>
    ///     Pressure at which tank should be considered 'low' such as for internals.
    /// </summary>
    [DataField]
    public float TankLowPressure = DefaultLowPressure;

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

    [DataField]
    public EntProtoId ToggleAction = "ActionToggleInternals";

    [DataField, AutoNetworkedField]
    public EntityUid? ToggleActionEntity;
}
