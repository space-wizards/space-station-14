using Robust.Server.Player;

namespace Content.Server.NewCon.Commands.Players;

[ConsoleCommand]
public sealed class PlayersCommand : ConsoleCommand
{
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    [CommandImplementation]
    public IEnumerable<IPlayerSession> Players()
        => _playerManager.ServerSessions;
}
