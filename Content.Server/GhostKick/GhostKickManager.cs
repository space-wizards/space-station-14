using Content.Server.Administration;
using Content.Shared.Administration;
using Content.Shared.GhostKick;
using Robust.Server.Player;
using Robust.Shared.Console;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.GhostKick;

// Handles logic for "ghost kicking".
// Basically we boot the client off the server without telling them, so the game shits itself.
// Hilariously isn't it?

public sealed class GhostKickManager
{
    [Dependency] private readonly IServerNetManager _netManager = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgGhostKick>();
    }

    public void DoDisconnect(INetChannel channel, string reason)
    {
        Timer.Spawn(TimeSpan.FromMilliseconds(100), () =>
        {
            if (!channel.IsConnected)
                return;

            // We do this so the client can set net.fakeloss 1 before getting ghosted.
            // This avoids it spamming messages at the server that cause warnings due to unconnected client.
            channel.SendMessage(new MsgGhostKick());

            Timer.Spawn(TimeSpan.FromMilliseconds(100), () =>
            {
                if (!channel.IsConnected)
                    return;

                // Actually just remove the client entirely.
                channel.Disconnect(reason, false);
            });
        });
    }
}

[AdminCommand(AdminFlags.Admin)]
public sealed class GhostKickCommand : IConsoleCommand
{
    public string Command => "ghostkick";
    public string Description => "Kick a client from the server as if their network just dropped.";
    public string Help => "Usage: ghostkick <Player> [Reason]";

    public void Execute(IConsoleShell shell, string argStr, string[] args)
    {
        if (args.Length < 1)
        {
            shell.WriteError("Need at least one argument");
            return;
        }

        var playerName = args[0];
        var reason = args.Length > 1 ? args[1] : "Ghost kicked by console";

        var players = IoCManager.Resolve<IPlayerManager>();
        var ghostKick = IoCManager.Resolve<GhostKickManager>();

        if (!players.TryGetSessionByUsername(playerName, out var player))
        {
            shell.WriteError($"Unable to find player: '{playerName}'.");
            return;
        }

        ghostKick.DoDisconnect(player.Channel, reason);
    }
}
