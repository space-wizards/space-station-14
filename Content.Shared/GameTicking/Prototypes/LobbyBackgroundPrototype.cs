using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared.GameTicking.Prototypes;

/// <summary>
/// Prototype for a lobby background the game can choose.
/// </summary>
[Prototype]
public sealed partial class LobbyBackgroundPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    /// The sprite to use as the background. This should ideally be 1920x1080.
    /// </summary>
    [DataField(required: true)]
    public ResPath Background = default!;

    /// <summary>
    /// The title of the background to be displayed in the lobby.
    /// </summary>
    [DataField]
    public LocId Title = "lobby-state-background-unknown-title";

    /// <summary>
    /// The artist who made the art for the background.
    /// </summary>
    [DataField]
    public LocId Artist = "lobby-state-background-unknown-artist";
}
