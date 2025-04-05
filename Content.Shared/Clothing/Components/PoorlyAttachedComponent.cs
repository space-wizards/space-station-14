using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Shared.Clothing.Components;

/// <summary>
/// Simulates this item not being securely attached to the wearer and capable of falling off
/// when the wearer falls down. How much attachment strength is lost per fall and per second
/// while worn can be configured. The item will not fall off on its own from passive strength
/// loss, only by falling, but passive loss can reduce the number of falls needed.
/// Also adds a verb to the item while worn to resecure it, which resets attachment strength to full.
/// </summary>
/// <remarks>
/// Attachment strength values range from fully attached at 1 to fully loose at 0.
/// </remarks>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
public sealed partial class PoorlyAttachedComponent : Component
{
    /// <summary>
    /// The time at which the item was last equipped or reattached.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField]
    [AutoPausedField]
    public TimeSpan AttachmentTime;

    /// <summary>
    /// The total amount of attachment strength that has been lost from the wearer falling over.
    /// </summary>
    [DataField, AutoNetworkedField]
    public float EventStrengthTotal;

    /// <summary>
    /// How long in seconds it will take for the item to lose all of its attachment strength
    /// if it is not reattached and the wearer does not fall down.
    /// </summary>
    /// <remarks>
    /// This will not directly cause the item to detach on its own without the wearer falling over.
    /// </remarks>
    [DataField]
    public TimeSpan PassiveDetachDuration = TimeSpan.FromMinutes(20);

    /// <summary>
    /// Shortcut calculation of how much attachment strength is lost per second.
    /// </summary>
    [ViewVariables]
    public float LossPerSecond => 1f / (float)PassiveDetachDuration.TotalSeconds;

    /// <summary>
    /// How much attachment strength is lost every time the wearer falls over.
    /// </summary>
    [DataField]
    public float LossPerFall = 0.2f;

    /// <summary>
    /// Are players other than the wearer allowed to reattach this item (via the strip window)?
    /// </summary>
    [DataField]
    public bool OthersCanReattach = true;

    /// <summary>
    /// If true, only the wearer and the reattacher will see the popup when reattaching this item.
    /// </summary>
    [DataField]
    public bool ReattachSilentToOthers;

    /// <summary>
    /// LocId of the message to display when the item falls off.
    /// </summary>
    [DataField]
    public LocId DetachPopup = "poorly-attached-detach-popup";

    /// <summary>
    /// LocId of the text to display on the reattach verb in the context menu.
    /// </summary>
    [DataField]
    public LocId ReattachVerbText = "poorly-attached-reattach-verb-default";

    /// <summary>
    /// Icon to display with the reattach verb.
    /// </summary>
    [DataField]
    public SpriteSpecifier? ReattachVerbIcon = new SpriteSpecifier.Texture(new("/Textures/Interface/VerbIcons/safety-pin.svg.192dpi.png"));

    /// <summary>
    /// LocId of the message to display to the user when they reattach this item on themself.
    /// </summary>
    [DataField]
    public LocId ReattachSelfPopupUser = "poorly-attached-reattach-self-popup-user-default";

    /// <summary>
    /// LocId of the message to display to nearby players when someone reattaches this item on themself.
    /// </summary>
    [DataField]
    public LocId ReattachSelfPopupOthers = "poorly-attached-reattach-self-popup-others-default";

    /// <summary>
    /// LocId of the message to display to the user when they reattach this item on someone else.
    /// </summary>
    [DataField]
    public LocId ReattachOtherPopupUser = "poorly-attached-reattach-other-popup-user-default";

    /// <summary>
    /// LocId of the message to display to the player wearing this item when someone else reattaches it on them.
    /// </summary>
    [DataField]
    public LocId ReattachOtherPopupWearer = "poorly-attached-reattach-other-popup-wearer-default";

    /// <summary>
    /// LocId of the message to display to nearby players when someone reattaches this item on someone else.
    /// </summary>
    [DataField]
    public LocId ReattachOtherPopupOthers = "poorly-attached-reattach-other-popup-others-default";
}
