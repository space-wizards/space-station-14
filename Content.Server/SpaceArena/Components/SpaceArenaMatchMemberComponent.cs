using Robust.Shared.Network;

namespace Content.Server.SpaceArena.Components;

[RegisterComponent, Access(typeof(SpaceArenaMatchSystem), typeof(SpaceArenaDeathMatchSystem))]
public sealed partial class SpaceArenaMatchMemberComponent : Component
{
    public EntityUid Match;

    public NetUserId Player;
}
