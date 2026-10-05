using Robust.Shared.GameStates;

namespace Content.Shared.NewPlayer;

/// <summary>
/// Players with this component can see the status icon related to <see cref="NewPlayerIconComponent"/>.
/// This component should only be provided to players in good standing, as it provides info on who is new to the game.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ShowNewPlayerIconComponent : Component
{
    // It only matters for the component owner client that they have it, so best not to share it to other clients.
    /// <inheritdoc />
    public override bool SendOnlyToOwner => true;
}
