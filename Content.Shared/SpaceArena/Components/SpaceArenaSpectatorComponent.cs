using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared.SpaceArena.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class SpaceArenaSpectatorComponent : Component
{
    public EntityUid Match;

    public NetUserId Player;
}
