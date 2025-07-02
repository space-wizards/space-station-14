using Robust.Shared.GameStates;

namespace Content.Shared.GameTicking.Components;

/// <summary>
/// Component that defines special lobby content (music and background) to be used when this game rule ends.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class SpecialLobbyContentComponent : Component
{
    /// <summary>
    /// The music track to play in the lobby after this game rule ends.
    /// If null, no special music will be set.
    /// </summary>
    [DataField("music")]
    public string? Music;

    /// <summary>
    /// The background image to display in the lobby after this game rule ends.
    /// If null, no special background will be set.
    /// </summary>
    [DataField("background")]
    public string? Background;
}
