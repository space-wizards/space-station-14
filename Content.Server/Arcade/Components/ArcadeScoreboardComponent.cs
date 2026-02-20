using Content.Server.Arcade.Prototypes;
using Content.Shared.Arcade.Systems;
using Robust.Shared.Prototypes;

namespace Content.Server.Arcade.Components;

[RegisterComponent]
public sealed partial class ArcadeScoreboardComponent : Component
{
    [DataField]
    public ProtoId<ArcadeScoreboardPrototype>? GlobalScoreboard = null;

    /// <summary>
    /// The maximum amount of top scores to keep track of for this scoreboard.
    /// </summary>
    [DataField]
    public int MaxEntries = 5;

    /// <summary>
    /// A list of all score entries for this scoreboard.
    /// </summary>
    [ViewVariables(VVAccess.ReadWrite)]
    public List<ArcadeHighScoreEntry> Scoreboard = new();
}
