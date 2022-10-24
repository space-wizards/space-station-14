using Content.Server.GameTicking.Rules.Configurations;
using JetBrains.Annotations;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server.StationEvents.Events;

public sealed class AgentActivation : StationEventSystem
{
    [Dependency] private readonly SleeperAgent _sleeperagent = true!;
    
    public override string Protoype => "AgentActivation";
        
    public override void Started()
    {
        base.Started();
        HashSet<EntityUid> stationsToNotify = new();
        List<SleeperAgent> agentList = new();
        foreach (var (agent, mobState) in EntityManager.EntityQuery<AntagComponent, MobStateComponent>())
        {
            if (!mobState.IsAlive())
                Mind.Add(activatedagent);
        }
    }
      var station = StationSystem.GetOwningStation(target.Owner);
    if(station == null) continue;
    stationsToNotify.Add((EntityUid) station);
}
