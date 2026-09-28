using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;

namespace Content.Server.StationEvents.Events;

/// <summary>
/// Handler for events that spawn a given entity at a random tile on the affected station.
/// </summary>
/// <seealso cref="RandomSpawnRuleComponent"/>
public sealed partial class RandomSpawnRule : StationEventSystem<RandomSpawnRuleComponent>
{
    protected override void Started(Entity<RandomSpawnRuleComponent, GameRuleComponent> ent, ref GameRuleStartedEvent args)
    {
        base.Started(ent, ref args);

        if (Station.TryFindRandomTile(out _, out _, out _, out var coords))
        {
            Sawmill.Info($"Spawning {ent.Comp1.Prototype} at {coords}");
            Spawn(ent.Comp1.Prototype, coords);
        }
    }
}
