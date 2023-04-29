using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;

namespace Content.Server.StationEvents.Events;

public sealed class RevenantSpawnRule : StationEventSystem<RevenantSpawnRuleComponent>
{
    protected override void Started(EntityUid uid, RevenantSpawnRuleComponent component, GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (TryFindRandomTile(out _, out _, out _, out var coords))
        {
            Sawmill.Info($"Spawning revenant at {coords}");
            Spawn(component.RevenantPrototype, coords);
        }
    }
}
