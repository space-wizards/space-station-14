using Content.Shared.DeviceLinking;
using Content.Shared.Doors.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Doors.Components;

/// <summary>
/// Companion component to DoorComponent that handles airlock-specific behavior -- wires, requiring power to operate, bolts, and allowing automatic closing.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(SharedAirlockSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class AirlockComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Powered;

    // Need to network airlock safety state to avoid mis-predicts when a door auto-closes as the client walks through the door.
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public bool Safety = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public bool EmergencyAccess = false;

    /// <summary>
    /// Used to hold the state of emergency access on the door prior to a station-destroying event.  This allows us to return to the saved state
    /// after the station-destroying threat is eliminated.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField, AutoNetworkedField]
    public bool PreDeltaAlertEmergencyAccessState = false;

    /// <summary>
    ///     Time before delta alert emergency access is reverted, in seconds, after a station-destroying threat is averted.
    /// </summary>
    [DataField("deltaemergencytimer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int PostDeltaAlertEmergencyAccessTimer = 10;

    /// <summary>
    ///     Time remaining until the door reverts its emergency access settings after a station-destroying threat is averted.
    /// </summary>
    [DataField("deltaemergencyaccesstimeremaining")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float PostDeltaAlertRemainingEmergencyAccessTimer;

    /// <summary>
    /// Tells us if the a station-destroying threat was recently averted on this airlock's grid.
    /// </summary>
    [DataField("deltarecentlyended")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DeltaAlertRecentlyEnded;

    /// <summary>
    /// Tells us if the a station-destroying threat is currently ongoing.
    /// </summary>
    [DataField("deltaongoing")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DeltaAlertOngoing;

    /// <summary>
    /// Determines how long to wait during a delta-level event before triggering emergency access.
    /// </summary>
    [DataField("deltaeadelaytime")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int DeltaAlertEmergencyAccessDelayTime = 180;

    /// <summary>
    /// Timer that keeps track of how long until the door enters emergency access.
    /// </summary>
    [DataField("deltaeadelaytimetimer")]
    [ViewVariables(VVAccess.ReadWrite)]
    public float DeltaAlertRemainingEmergencyAccessTimer;
    /// <summary>
    /// Determines if the door is currently under delta-level emergency access rules.
    /// </summary>
    [DataField("deltaeaenabled")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool DeltaEmergencyAccessEnabled;



    /// <summary>
    /// Pry modifier for a powered airlock.
    /// Most anything that can pry powered has a pry speed bonus,
    /// so this default is closer to 6 effectively on e.g. jaws (9 seconds when applied to other default.)
    /// </summary>
    [DataField]
    public float PoweredPryModifier = 9f;

    /// <summary>
    /// Whether the maintenance panel should be visible even if the airlock is opened.
    /// </summary>
    [DataField]
    public bool OpenPanelVisible = false;

    /// <summary>
    /// Whether the airlock should stay open if the airlock was clicked.
    /// If the airlock was bumped into it will still auto close.
    /// </summary>
    [DataField]
    public bool KeepOpenIfClicked = false;

    /// <summary>
    /// Whether the airlock should auto close. This value is reset every time the airlock closes.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public bool AutoClose = true;

    /// <summary>
    /// Delay until an open door automatically closes.
    /// </summary>
    [DataField]
    public TimeSpan AutoCloseDelay = TimeSpan.FromSeconds(5f);

    /// <summary>
    /// Multiplicative modifier for the auto-close delay. Can be modified by hacking the airlock wires. Setting to
    /// zero will disable auto-closing.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public float AutoCloseDelayModifier = 1.0f;

    /// <summary>
    /// The receiver port for turning off automatic closing.
    /// </summary>
    [DataField(customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string AutoClosePort = "AutoClose";

    #region Graphics

    /// <summary>
    /// Whether the door lights should be visible.
    /// </summary>
    [DataField]
    public bool OpenUnlitVisible = false;

    /// <summary>
    /// Whether the door should display emergency access lights.
    /// </summary>
    [DataField]
    public bool EmergencyAccessLayer = true;

    /// <summary>
    /// Whether or not to animate the panel when the door opens or closes.
    /// </summary>
    [DataField]
    public bool AnimatePanel = true;

    /// <summary>
    /// The sprite state used to animate the airlock frame when the airlock opens.
    /// </summary>
    [DataField]
    public string OpeningSpriteState = "opening_unlit";

    /// <summary>
    /// The sprite state used to animate the airlock panel when the airlock opens.
    /// </summary>
    [DataField]
    public string OpeningPanelSpriteState = "panel_opening";

    /// <summary>
    /// The sprite state used to animate the airlock frame when the airlock closes.
    /// </summary>
    [DataField]
    public string ClosingSpriteState = "closing_unlit";

    /// <summary>
    /// The sprite state used to animate the airlock panel when the airlock closes.
    /// </summary>
    [DataField]
    public string ClosingPanelSpriteState = "panel_closing";

    /// <summary>
    /// The sprite state used for the open airlock lights.
    /// </summary>
    [DataField]
    public string OpenSpriteState = "open_unlit";

    /// <summary>
    /// The sprite state used for the closed airlock lights.
    /// </summary>
    [DataField]
    public string ClosedSpriteState = "closed_unlit";

    /// <summary>
    /// The sprite state used for the 'access denied' lights animation.
    /// </summary>
    [DataField]
    public string DenySpriteState = "deny_unlit";

    /// <summary>
    /// How long the animation played when the airlock denies access is in seconds.
    /// </summary>
    [DataField]
    public float DenyAnimationTime = 0.3f;

    /// <summary>
    /// Pry modifier for a bolted airlock.
    /// Currently only zombies can pry bolted airlocks.
    /// </summary>
    [DataField]
    public float BoltedPryModifier = 3f;

    #endregion Graphics
}
