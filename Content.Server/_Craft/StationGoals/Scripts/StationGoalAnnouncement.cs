using System.Threading;
using Content.Server._Craft.Utils;
using Content.Server.Chat.Systems;
using Robust.Shared.Prototypes;
using Timer = Robust.Shared.Timing.Timer;

namespace Content.Server._Craft.StationGoals.Scipts;


public sealed class StationGoalAnnouncement : IStationGoalScript
{
    private CancellationTokenSource? token = null;

    public void PerformAction(StationGoalPrototype stationGoal, IPrototypeManager prototypeManager, EntitySystem entitySystem)
    {
        token = new CancellationTokenSource();
        Timer.Spawn(
            milliseconds: 30000,
            onFired: () =>
            {
                var chatSystem = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<ChatSystem>();
                ChatUtils.SendLocMessageFromCentcom(chatSystem, stationGoal.Text, null);
            },
            cancellationToken: token.Token
        );
    }

    public void Cleanup()
    {
        token?.Cancel();
    }
}
