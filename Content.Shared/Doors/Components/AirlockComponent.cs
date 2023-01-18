using System.Threading;
using Content.Shared.Doors.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared.Doors.Components;

/// <summary>
/// Companion component to DoorComponent that handles airlock-specific behavior -- wires, requiring power to operate, bolts, and allowing automatic closing.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedAirlockSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed class AirlockComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("safety")]
    public bool Safety = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("emergencyAccess")]
    public bool EmergencyAccess = false;

    /// <summary>
    /// Sound to play when the bolts on the airlock go up.
    /// </summary>
    [DataField("boltUpSound")]
    public SoundSpecifier BoltUpSound = new SoundPathSpecifier("/Audio/Machines/boltsup.ogg");

    /// <summary>
    /// Sound to play when the bolts on the airlock go down.
    /// </summary>
    [DataField("boltDownSound")]
    public SoundSpecifier BoltDownSound = new SoundPathSpecifier("/Audio/Machines/boltsdown.ogg");

    /// <summary>
    /// Pry modifier for a powered airlock.
    /// Most anything that can pry powered has a pry speed bonus,
    /// so this default is closer to 6 effectively on e.g. jaws (9 seconds when applied to other default.)
    /// </summary>
    [DataField("poweredPryModifier")]
    public readonly float PoweredPryModifier = 9f;

    /// <summary>
    /// Whether the maintenance panel should be visible even if the airlock is opened.
    /// </summary>
    [DataField("openPanelVisible")]
    public bool OpenPanelVisible = false;

    /// <summary>
    /// Whether the airlock should stay open if the airlock was clicked.
    /// If the airlock was bumped into it will still auto close.
    /// </summary>
    [DataField("keepOpenIfClicked")]
    public bool KeepOpenIfClicked = false;

    public bool BoltsDown;

    public bool BoltLightsEnabled = true;

    /// <summary>
    /// True if the bolt wire is cut, which will force the airlock to always be bolted as long as it has power.
    /// </summary>
    [ViewVariables]
    public bool BoltWireCut;

    /// <summary>
    /// Whether the airlock should auto close. This value is reset every time the airlock closes.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AutoClose = true;

    /// <summary>
    /// Delay until an open door automatically closes.
    /// </summary>
    [DataField("autoCloseDelay")]
    public TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// Multiplicative modifier for the auto-close delay. Can be modified by hacking the airlock wires. Setting to
    /// zero will disable auto-closing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AutoCloseDelayModifier = 1.0f;
}

[Serializable, NetSerializable]
public sealed class AirlockComponentState : ComponentState
{
    public readonly bool Safety;

    public AirlockComponentState(bool safety)
    {
        Safety = safety;
    }
}
