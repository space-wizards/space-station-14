using System.Numerics;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Spawners.Components;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Server.Spawners.EntitySystems
{
    [UsedImplicitly]
    public sealed class ConditionalSpawnerSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;
        [Dependency] private readonly GameTicker _ticker = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GameRuleStartedEvent>(OnRuleStarted);
            SubscribeLocalEvent<ConditionalSpawnerComponent, MapInitEvent>(OnCondSpawnMapInit);
            SubscribeLocalEvent<RandomSpawnerComponent, MapInitEvent>(OnRandSpawnMapInit);
        }

        private void OnCondSpawnMapInit(EntityUid uid, ConditionalSpawnerComponent component, MapInitEvent args)
        {
            TrySpawn(uid, component);
        }

        private void OnRandSpawnMapInit(EntityUid uid, RandomSpawnerComponent component, MapInitEvent args)
        {
            Spawn(uid, component);
            if (component.DeleteSpawnerAfterSpawn)
                QueueDel(uid);
        }

        private void OnRuleStarted(ref GameRuleStartedEvent args)
        {
            var query = EntityQueryEnumerator<ConditionalSpawnerComponent>();
            while (query.MoveNext(out var uid, out var spawner))
            {
                RuleStarted(uid, spawner, args);
            }
        }

        public void RuleStarted(EntityUid uid, ConditionalSpawnerComponent component, GameRuleStartedEvent obj)
        {
            if (component.GameRules.Contains(obj.RuleId))
                Spawn(uid, component);
        }

        private void TrySpawn(EntityUid uid, ConditionalSpawnerComponent component)
        {
            if (component.GameRules.Count == 0)
            {
                Spawn(uid, component);
                return;
            }

            foreach (var rule in component.GameRules)
            {
                if (!_ticker.IsGameRuleActive(rule))
                    continue;
                Spawn(uid, component);
                return;
            }
        }

        private void Spawn(EntityUid uid, ConditionalSpawnerComponent component)
        {
            if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
                return;

            if (component.Prototypes.Count == 0)
            {
                Logger.Warning($"Prototype list in ConditionalSpawnComponent is empty! Entity: {ToPrettyString(uid)}");
                return;
            }

            if (!Deleted(uid))
                EntityManager.SpawnEntity(_robustRandom.Pick(component.Prototypes), Transform(uid).Coordinates);
        }

        private void Spawn(EntityUid uid, RandomSpawnerComponent component)
        {
            if (component.RarePrototypes.Count > 0 && (component.RareChance == 1.0f || _robustRandom.Prob(component.RareChance)))
            {
                EntityManager.SpawnEntity(_robustRandom.Pick(component.RarePrototypes), Transform(uid).Coordinates);
                return;
            }

            if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
                return;

            if (component.Prototypes.Count == 0)
            {
                Logger.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {ToPrettyString(uid)}");
                return;
            }

            if (Deleted(uid))
                return;

            var offset = component.Offset;
            var xOffset = _robustRandom.NextFloat(-offset, offset);
            var yOffset = _robustRandom.NextFloat(-offset, offset);

            var coordinates = Transform(uid).Coordinates.Offset(new Vector2(xOffset, yOffset));

            EntityManager.SpawnEntity(_robustRandom.Pick(component.Prototypes), coordinates);
        }
    }
}
