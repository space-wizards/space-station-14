using Content.Shared.GhostKick;
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

    public void DoDisconnect(INetChannel channel)
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
                channel.Disconnect("Tripped over a kick mine, crashed through the fourth wall", false);
            });
        });
    }
}
