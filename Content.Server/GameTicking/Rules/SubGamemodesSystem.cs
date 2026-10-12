using Content.Server.GameTicking.Rules.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.GameTicking.Rules;
using Content.Shared.Storage;

namespace Content.Server.GameTicking.Rules;

/// <summary>
/// A handler for rules adding one or more additional rules when a first is added.
/// </summary>
/// <seealso cref="SubGamemodesComponent"/>
public sealed partial class SubGamemodesSystem : GameRuleSystem<SubGamemodesComponent>
{
    protected override void Added(Entity<SubGamemodesComponent, GameRuleComponent> ent, ref GameRuleAddedEvent args)
    {
        var picked = EntitySpawnCollection.GetSpawns(ent.Comp1.Rules, RobustRandom);
        foreach (var id in picked)
        {
            if (GameTicker.IsIgnored(id))
                continue;

            Log.Info($"Starting gamerule {id} as a subgamemode of {ToPrettyString(ent.Owner):rule}");
            GameTicker.AddGameRule(id);
        }
    }
}
