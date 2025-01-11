using Content.Server.GameTicking.Rules.Components;
using Content.Server.StationEvents.Components;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;
using Robust.Shared.Random;
using Content.Server.Deathwhale;

namespace Content.Server.StationEvents.Events
{
    public sealed class OceanSpawnRule : StationEventSystem<OceanSpawnRuleComponent>
    {
        protected override void Started(EntityUid uid, OceanSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleStartedEvent args)
        {
            base.Started(uid, comp, gameRule, args);

            float Amount = comp.Amount;

            if (!TryGetRandomStation(out var station))
            {
                return;
            }

            var locations = EntityQueryEnumerator<DeathWhaleSpawnLocationComponent, TransformComponent>();
            var validLocations = new List<EntityCoordinates>();

            while (locations.MoveNext(out var _, out var spawnLocation, out var transform))
            {
                validLocations.Add(transform.Coordinates);

                if (comp.CurrentAmount >= Amount) break;
                Spawn(comp.Prototype, transform.Coordinates);
                comp.CurrentAmount += 1;
            }

            if (validLocations.Count == 0)
            {
                return;
            }

            foreach (var location in validLocations)
            {
                if (comp.CurrentAmount >= Amount) break;

                Spawn(comp.Prototype, location);
                comp.CurrentAmount += 1;
            }
        }

        protected override void Ended(EntityUid uid, OceanSpawnRuleComponent comp, GameRuleComponent gameRule, GameRuleEndedEvent args)
        {
            base.Ended(uid, comp, gameRule, args);
            comp.CurrentAmount = 0f;

            foreach (var whales in EntityManager.EntityQuery<DeathWhaleComponent>())
            {
                var whaleUid = whales.Owner;  // Renamed to avoid conflict with method 'uid'
                QueueDel(whaleUid);  // Deleting the whale entity
            }
        }
    }
}
