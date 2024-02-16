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
    [DataField]
    public string Message;

    /// <summary>
    /// Permission to customize message text
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public bool AllowEditingMessage = true;

    /// <summary>
    /// War declarement text color
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public Color Color = Color.Red;

    /// <summary>
    /// War declarement sound file path
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/Announcements/war.ogg");

    /// <summary>
    /// Fluent ID for the declarement title
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    [DataField]
    public LocId Title = "comms-console-announcement-title-nukie";
}
