using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.Prayer;

/// <summary>
/// Allows an entity to be prayed on in the context menu
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class PrayableComponent : Component
{
    /// <summary>
    /// If bible users are only allowed to use this prayable entity
    /// </summary>
    [DataField("bibleUserOnly")]
    [ViewVariables(VVAccess.ReadWrite)]
    public bool BibleUserOnly;

    /// <summary>
    /// Message given to user to notify them a message was sent
    /// </summary>
    [DataField("sentMessage")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string SentMessage = "prayer-popup-notify-pray-sent";

    /// <summary>
    /// Prefix used in the notification to admins
    /// </summary>
    [DataField("notifiactionPrefix")]
    [ViewVariables(VVAccess.ReadWrite)]
    public string NotificationPrefix = "prayer-chat-notify-pray";

    /// <summary>
    /// Used in window title and context menu
    /// </summary>
    [DataField("verb")]
    [ViewVariables(VVAccess.ReadOnly)]
    public string Verb = "prayer-verbs-pray";

    /// <summary>
    /// Context menu image
    /// </summary>
    [DataField("verbImage")]
    [ViewVariables(VVAccess.ReadOnly)]
    public SpriteSpecifier? VerbImage = new SpriteSpecifier.Texture(new ("/Textures/Interface/pray.svg.png"));
}
