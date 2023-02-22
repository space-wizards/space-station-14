using Robust.Shared.GameStates;

namespace Content.Shared.HotPotato;

[RegisterComponent, NetworkedComponent]
public sealed class HotPotatoComponent : Component
{
    public bool CanTransfer = true;
}
