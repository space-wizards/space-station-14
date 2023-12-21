using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents;
using Robust.Shared.Configuration;
using Robust.Shared.Random;

namespace Content.Server.GameTicking.Rules;

public sealed class SurvivalRuleSystem : GameRuleSystem<SurvivalRuleComponent>
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EventManagerSystem _event = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    protected override void Started(EntityUid uid, SurvivalRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_event.EventsEnabled)
            return;

        var query = EntityQueryEnumerator<SurvivalRuleComponent, GameRuleComponent>();
        while (query.MoveNext(out var uid, out var scheduler, out var gameRule))
        {
            if (!GameTicker.IsGameRuleActive(uid, gameRule))
                return;

        }
    }
}
