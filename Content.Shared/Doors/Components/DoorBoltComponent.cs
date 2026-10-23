using Content.Shared.Doors.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;

namespace Content.Shared.Doors.Components;

/// <summary>
/// Companion component to DoorComponent that handles bolt-specific behavior.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoorSystem))]
[AutoGenerateComponentState]
public sealed partial class DoorBoltComponent : Component
{
    /// <summary>
    /// Sound to play when the bolts on the airlock go up.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BoltUpSound = new SoundPathSpecifier("/Audio/Machines/boltsup.ogg");

    /// <summary>
    /// Sound to play when the bolts on the airlock go down.
    /// </summary>
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BoltDownSound = new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg");

    /// <summary>
    /// Whether the door bolts are currently deployed.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BoltsDown;

    /// <summary>
    /// Whether the bolt lights are currently enabled.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BoltLightsEnabled = true;

    /// <summary>
    /// True if the bolt wire is cut, which will force the airlock to always be bolted as long as it has power.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool BoltWireCut;

    /// <summary>
    /// Used for prediction. true if the door has power.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Powered;
}
