using Content.Shared.LandMines;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Server.LandMines;

public sealed class KickMineManager
{
    [Dependency] private readonly IServerNetManager _netManager = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<MsgKickMineDisconnect>();
    }

    public void DoDisconnect(INetChannel channel)
    {
        Timer.Spawn(TimeSpan.FromMilliseconds(100), () =>
        {
            if (!channel.IsConnected)
                return;

            channel.SendMessage(new MsgKickMineDisconnect());

            Timer.Spawn(TimeSpan.FromMilliseconds(100), () =>
            {
                if (!channel.IsConnected)
                    return;

                channel.Disconnect("Tripped over a kick mine, crashed through the fourth wall", false);
            });
        });
    }
}
