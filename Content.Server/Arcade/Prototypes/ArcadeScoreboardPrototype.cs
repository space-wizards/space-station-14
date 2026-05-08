using Robust.Shared.Prototypes;

namespace Content.Server.Arcade.Prototypes;

/// <summary>
///     Represents a local and global scoreboard for a particular arcade game.
/// </summary>
/// <remarks>
///     Arcade games have three kinds of scoreboards: machine, local, and global.
///     Machine scoreboards are per-arcade machine entity; local scoreboards are per-session; global scoreboards are all-time.
/// </remarks>
[Prototype]
public sealed partial class ArcadeScoreboardPrototype : IPrototype
{

    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// The maximum amount of top scores to keep track of for this scoreboard.
    /// </summary>
    /// <remarks>
    /// Will use the fallback count in <see cref="CCVars.FallbackScoreboardEntriesCount"/> if this is null.
    /// </remarks>
    [DataField]
    public int? MaxEntries = null;
}
