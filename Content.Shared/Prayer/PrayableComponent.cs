using Robust.Shared.Audio;
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
    /// The sound played for admins when a prayer is sent
	/// </summary>
    [DataField("soundForGod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public SoundSpecifier SoundForGod = new SoundPathSpecifier("/Audio/Effects/plehnimda.ogg");

    /// <summary>
    /// The volume of the sound played for admins when a prayer is sent
	/// </summary>
    [DataField("volumeForGod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public int VolumeForGod = 0;
	
    /// <summary>
    /// The last time a sound was played for admins
	/// </summary>
    [DataField("nextRing")]
    [ViewVariables(VVAccess.ReadOnly)]
    public TimeSpan NextRing;

    /// <summary>
    /// The cooldown on playing sounds for admins
	/// </summary>
    [DataField("ringPeriod")]
    [ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan RingPeriod = TimeSpan.FromSeconds(10f);

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
