using Content.Shared.StatusIcon;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared.NewPlayer;

/// <summary>
/// Players with this component can be seen by players with <see cref="ShowNewPlayerIconComponent"/>.
/// It is used to indicate a player is new to the game.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class NewPlayerIconComponent : Component
{
    /// <summary>
    /// The status icon corresponding to the new player icon.
    /// </summary>
    [DataField]
    public ProtoId<NewPlayerIconPrototype> StatusIcon { get; set; } = "NewPlayerIcon";

    // We only send these out to ShowNewPlayerIconComponent users, to avoid malicious new player detection.
    public override bool SessionSpecific => true;
}
