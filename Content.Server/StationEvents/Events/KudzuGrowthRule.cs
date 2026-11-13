using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that spawn kudzu on a random tile on a station.
/// </summary>
/// <seealso cref="KudzuGrowthRuleComponent"/>
public sealed partial class KudzuGrowthRule : StationEventSystem<KudzuGrowthRuleComponent>
{
    protected override void Started(Entity<KudzuGrowthRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        // Pick a place to plant the kudzu.
        if (!Station.TryFindRandomTile(out var targetTile, out _, out var targetGrid, out var targetCoords))
            return;
        Spawn("Kudzu", targetCoords);
        Sawmill.Info($"Spawning a Kudzu at {targetTile} on {targetGrid}");
    }
}
