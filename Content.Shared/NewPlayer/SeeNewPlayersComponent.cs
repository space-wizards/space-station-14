using Robust.Shared.GameStates;
using Robust.Shared.Utility;

namespace Content.Shared.NewPlayer;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class SeeNewPlayersComponent : Component
{
    [DataField, AutoNetworkedField]
    public Dictionary<NewPlayerVisuals, SpriteSpecifier.Rsi> LabelSprites = new()
    {
        { NewPlayerVisuals.NewTotal, new SpriteSpecifier.Rsi(new ResPath("Objects/Misc/new_player_marker.rsi"), "new_player_marker") },
    };
}
