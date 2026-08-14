using Robust.Shared.Network;
using Robust.Shared.Prototypes;

namespace Content.Server.SpaceArena.Components;

[RegisterComponent, Access(typeof(SpaceArenaLobbySystem), typeof(SpaceArenaLobbyTerminalSystem))]
public sealed partial class SpaceArenaPlayerLobbyComponent : Component
{
    public NetUserId Host;

    public string HostName = string.Empty;

    public EntProtoId Mode;
}
