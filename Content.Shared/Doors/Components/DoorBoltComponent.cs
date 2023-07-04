using Content.Shared.Doors.Systems;
using Content.Shared.MachineLinking;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Doors.Components;

/// <summary>
/// Companion component to DoorComponent that handles bolt-specific behavior.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedDoorBoltSystem))]
public sealed class DoorBoltComponent : Component
{
    /// <summary>
    /// Sound to play when the bolts on the airlock go up.
    /// </summary>
    [DataField("boltUpSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BoltUpSound = new SoundPathSpecifier("/Audio/Machines/boltsup.ogg");

    /// <summary>
    /// Sound to play when the bolts on the airlock go down.
    /// </summary>
    [DataField("boltDownSound"), ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier BoltDownSound = new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg");

    /// <summary>
    /// Whether the door bolts are currently deployed.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool BoltsDown;

    /// <summary>
    /// Whether the bolt lights are currently enabled.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool BoltLightsEnabled = true;

    /// <summary>
    /// True if the bolt wire is cut, which will force the airlock to always be bolted as long as it has power.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool BoltWireCut;
}
