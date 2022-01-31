using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
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

            SubscribeLocalEvent<GameRuleAddedEvent>(OnRuleAdded);
            SubscribeLocalEvent<ConditionalSpawnerComponent, MapInitEvent>(OnCondSpawnMapInit);
            SubscribeLocalEvent<RandomSpawnerComponent, MapInitEvent>(OnRandSpawnMapInit);
        }

        private void OnCondSpawnMapInit(EntityUid uid, ConditionalSpawnerComponent component, MapInitEvent args)
        {
            TrySpawn(component);
        }

        private void OnRandSpawnMapInit(EntityUid uid, RandomSpawnerComponent component, MapInitEvent args)
        {
            Spawn(component);
            EntityManager.QueueDeleteEntity(uid);
        }

        private void OnRuleAdded(GameRuleAddedEvent args)
        {
            foreach (var spawner in EntityManager.EntityQuery<ConditionalSpawnerComponent>())
            {
                RuleAdded(spawner, args);
            }
        }

        public void RuleAdded(ConditionalSpawnerComponent component, GameRuleAddedEvent obj)
        {
            if(component.GameRules.Contains(obj.Rule.ID))
                Spawn(component);
        }

        private void TrySpawn(ConditionalSpawnerComponent component)
        {
            if (component.GameRules.Count == 0)
            {
                Spawn(component);
                return;
            }

            foreach (var rule in component.GameRules)
            {
                if (!_ticker.HasGameRule(rule)) continue;
                Spawn(component);
                return;
            }
        }

        private void Spawn(ConditionalSpawnerComponent component)
        {
            if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
                return;

            if (component.Prototypes.Count == 0)
            {
                Logger.Warning($"Prototype list in ConditionalSpawnComponent is empty! Entity: {component.Owner}");
                return;
            }

            if (!Deleted(component.Owner))
                EntityManager.SpawnEntity(_robustRandom.Pick(component.Prototypes), Transform(component.Owner).Coordinates);
        }

        private void Spawn(RandomSpawnerComponent component)
        {
            if (component.RarePrototypes.Count > 0 && (component.RareChance == 1.0f || _robustRandom.Prob(component.RareChance)))
            {
                EntityManager.SpawnEntity(_robustRandom.Pick(component.RarePrototypes), Transform(component.Owner).Coordinates);
                return;
            }

            if (component.Chance != 1.0f && !_robustRandom.Prob(component.Chance))
                return;

            if (component.Prototypes.Count == 0)
            {
                Logger.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {component.Owner}");
                return;
            }

            if (Deleted(component.Owner)) return;

            var offset = component.Offset;
            var xOffset = _robustRandom.NextFloat(-offset, offset);
            var yOffset = _robustRandom.NextFloat(-offset, offset);

            var coordinates = Transform(component.Owner).Coordinates.Offset(new Vector2(xOffset, yOffset));

            EntityManager.SpawnEntity(_robustRandom.Pick(component.Prototypes), coordinates);
        }
    }
}
