using Content.Server.GameTicking.Rules.Components;
using Content.Shared.CCVar;
using Content.Shared.GameTicking.Components;
using Content.Shared.Storage;
using Robust.Shared.Configuration;

namespace Content.Server.GameTicking.Rules;

public sealed class SubGamemodesSystem : GameRuleSystem<SubGamemodesComponent>
{
    [Dependency] private readonly GameTicker _game = default!;

    protected override void Added(EntityUid uid, SubGamemodesComponent comp, GameRuleComponent rule, GameRuleAddedEvent args)
    {
        var picked = EntitySpawnCollection.GetSpawns(comp.Rules, RobustRandom);
        foreach (var id in picked)
        {
            if (_game.IgnoredPresets.Contains(id))
                continue;

            Log.Info($"Starting gamerule {id} as a subgamemode of {ToPrettyString(uid):rule}");
            GameTicker.AddGameRule(id);
        }
    }
}
