using Content.Shared.DeviceLinking;
using Content.Shared.Doors.Systems;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Shared.Doors.Components;

/// <summary>
/// Companion component to DoorComponent that handles airlock-specific behavior -- wires, requiring power to operate, bolts, and allowing automatic closing.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(SharedAirlockSystem), Friend = AccessPermissions.ReadWriteExecute, Other = AccessPermissions.Read)]
public sealed partial class AirlockComponent : Component
{
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("safety")]
    public bool Safety = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("emergencyAccess")]
    public bool EmergencyAccess = false;

    /// <summary>
    /// Pry modifier for a powered airlock.
    /// Most anything that can pry powered has a pry speed bonus,
    /// so this default is closer to 6 effectively on e.g. jaws (9 seconds when applied to other default.)
    /// </summary>
    [DataField("poweredPryModifier")]
    public float PoweredPryModifier = 9f;

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

    /// <summary>
    /// The receiver port for turning off automatic closing.
    /// </summary>
    [DataField("autoClosePort", customTypeSerializer: typeof(PrototypeIdSerializer<SinkPortPrototype>))]
    public string AutoClosePort = "AutoClose";

    #region Graphics

    /// <summary>
    /// Whether the door lights should be visible.
    /// </summary>
    [DataField("openUnlitVisible")]
    public bool OpenUnlitVisible = false;

    /// <summary>
    /// Whether the door should display emergency access lights.
    /// </summary>
    [DataField("emergencyAccessLayer")]
    public bool EmergencyAccessLayer = true;

    /// <summary>
    /// Whether or not to animate the panel when the door opens or closes.
    /// </summary>
    [DataField("animatePanel")]
    public bool AnimatePanel = true;

    /// <summary>
    /// The sprite state used to animate the airlock frame when the airlock opens.
    /// </summary>
    [DataField("openingSpriteState")]
    public string OpeningSpriteState = "opening_unlit";

    /// <summary>
    /// The sprite state used to animate the airlock panel when the airlock opens.
    /// </summary>
    [DataField("openingPanelSpriteState")]
    public string OpeningPanelSpriteState = "panel_opening";

    /// <summary>
    /// The sprite state used to animate the airlock frame when the airlock closes.
    /// </summary>
    [DataField("closingSpriteState")]
    public string ClosingSpriteState = "closing_unlit";

    /// <summary>
    /// The sprite state used to animate the airlock panel when the airlock closes.
    /// </summary>
    [DataField("closingPanelSpriteState")]
    public string ClosingPanelSpriteState = "panel_closing";

    /// <summary>
    /// The sprite state used for the open airlock lights.
    /// </summary>
    [DataField("openSpriteState")]
    public string OpenSpriteState = "open_unlit";

    /// <summary>
    /// The sprite state used for the closed airlock lights.
    /// </summary>
    [DataField("closedSpriteState")]
    public string ClosedSpriteState = "closed_unlit";

    /// <summary>
    /// The sprite state used for the 'access denied' lights animation.
    /// </summary>
    [DataField("denySpriteState")]
    public string DenySpriteState = "deny_unlit";

    /// <summary>
    /// How long the animation played when the airlock denies access is in seconds.
    /// </summary>
    [DataField("denyAnimationTime")]
    public float DenyAnimationTime = 0.3f;

    #endregion Graphics
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
