using Content.Server.Arcade.Prototypes;
using Content.Shared.Arcade.BlockGame;
using Robust.Shared.Prototypes;

namespace Content.Server.Arcade.Components;

[RegisterComponent]
public sealed partial class ArcadeScoreboardComponent : Component
{
    [DataField]
    public ProtoId<ArcadeScoreboardPrototype>? GlobalScoreboard = null;

    public List<BlockGameHighScoreEntry> Scoreboard = new();
}
