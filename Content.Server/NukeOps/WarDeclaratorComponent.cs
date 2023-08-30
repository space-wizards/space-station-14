using Robust.Shared.Audio;

namespace Content.Server.NukeOps;

/// <summary>
/// Used with NukeOps game rule to send war declaration announcement
/// </summary>
[RegisterComponent]
public sealed partial class WarDeclaratorComponent : Component
{
    /// <summary>
    /// Custom war declaration message. If empty, use default.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("message")]
    public string Message;

    /// <summary>
    /// Permission to customize message text
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("allowEditingMessage")]
    public bool AllowEditingMessage = true;

    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("maxMessageLength")]
    public int MaxMessageLength = 512;

    /// <summary>
    /// War declarement text color
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("color")]
    public Color DeclarementColor = Color.Red;

    /// <summary>
    /// War declarement sound file path
    /// </summary>
    [DataField("sound")]
    public SoundSpecifier DeclarementSound = new SoundPathSpecifier("/Audio/Announcements/war.ogg");

    /// <summary>
    /// Fluent ID for the declarement title
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField("title")]
    public string DeclarementTitle = "comms-console-announcement-title-nukie";
}
