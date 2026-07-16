using Robust.Shared.GameStates;

namespace Content.Shared.NewPlayer;

/// <summary>
/// Players with this component can see the status icon related to <see cref="NewPlayerIconComponent"/>.
/// That component is used to indicate a player is new to the game.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowNewPlayerIconComponent : Component
{
    public override bool SendOnlyToOwner => true;
}
