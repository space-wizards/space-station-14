using Robust.Shared.Prototypes;

namespace Content.Server.Arcade.Prototypes;

/// <summary>
/// This prototype represents a particular arcade machine's global scoreboard.
/// There are two arcade machine scoreboards - local, and global. Local is per-machine, while global is server-wide.
/// Both are cleared once the round ends.
/// </summary>
[Prototype]
public sealed partial class ArcadeScoreboardPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; private set; } = default!;
}
