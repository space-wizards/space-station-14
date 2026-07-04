using Content.Server.GameTicking;
using Content.Server.Spawners.Components;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using JetBrains.Annotations;
using Robust.Server.GameObjects;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

// TODO: This whole system is a mess. A lot of this should be marked obsolete.
// TODO: It should probably use interfaces with entity tables *if* more than one component is needed.
// TODO: Remove the TransformSystem Dependency when engine SpawnAtPosition EntityCoordinates override is fixed.
namespace Content.Server.Spawners.EntitySystems
{
    [UsedImplicitly]
    public sealed partial class ConditionalSpawnerSystem : EntitySystem
    {
        [Dependency] private IRobustRandom _robustRandom = default!;
        [Dependency] private GameTicker _ticker = default!;
        [Dependency] private EntityTableSystem _entityTable = default!;
        [Dependency] private TransformSystem _xform = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<GameRuleStartedEvent>(OnRuleStarted);
            SubscribeLocalEvent<ConditionalSpawnerComponent, MapInitEvent>(OnCondSpawnMapInit);
            SubscribeLocalEvent<RandomSpawnerComponent, MapInitEvent>(OnRandSpawnMapInit);
            SubscribeLocalEvent<EntityTableSpawnerComponent, MapInitEvent>(OnEntityTableSpawnMapInit);
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

        private void OnEntityTableSpawnMapInit(Entity<EntityTableSpawnerComponent> ent, ref MapInitEvent args)
        {
            Spawn(ent);
            if (ent.Comp.DeleteSpawnerAfterSpawn && !TerminatingOrDeleted(ent) && Exists(ent))
                QueueDel(ent);
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
                Log.Warning($"Prototype list in ConditionalSpawnComponent is empty! Entity: {ToPrettyString(uid)}");
                return;
            }

            if (Deleted(uid))
                return;

            var xform = Transform(uid);
            var coords = _xform.GetMapCoordinates(uid, xform);
            var rotation = _xform.GetWorldRotation(xform);

            Spawn(_robustRandom.Pick(component.Prototypes), coords, rotation: rotation);
        }

        private void Spawn(EntityUid uid, RandomSpawnerComponent component)
        {
            if (Deleted(uid))
                return;

            if (GetPrototype((uid, component)) is not { } proto)
                return;

            var offset = component.Offset;
            var vOffset = _robustRandom.NextVector2Box(-offset, offset);

            var xform = Transform(uid);
            var coords = _xform.GetMapCoordinates(uid, xform).Offset(vOffset);
            var rotation = _xform.GetWorldRotation(xform);

            Spawn(proto, coords, rotation: rotation);
        }

        private EntProtoId? GetPrototype(Entity<RandomSpawnerComponent> spawner)
        {
            if (GetPrototypes(spawner) is not { } list)
                return null;

            return _robustRandom.Pick(list);
        }

        private List<EntProtoId>? GetPrototypes(Entity<RandomSpawnerComponent> spawner)
        {
            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (spawner.Comp.RarePrototypes.Count > 0 &&
                (spawner.Comp.RareChance == 1.0f || _robustRandom.Prob(spawner.Comp.RareChance)))
            {
                return spawner.Comp.RarePrototypes;
            }

            if (spawner.Comp.Prototypes.Count == 0)
            {
                Log.Warning($"Prototype list in RandomSpawnerComponent is empty! Entity: {ToPrettyString(spawner)}");
                return null;
            }

            // ReSharper disable once CompareOfFloatsByEqualityOperator
            if (spawner.Comp.Chance == 1.0f || !_robustRandom.Prob(spawner.Comp.Chance))
            {
                return spawner.Comp.Prototypes;
            }

            return null;
        }

        private void Spawn(Entity<EntityTableSpawnerComponent> ent)
        {
            if (TerminatingOrDeleted(ent) || !Exists(ent))
                return;

            var xform = Transform(ent);
            var coords = _xform.GetMapCoordinates(ent, xform);
            var rotation = _xform.GetWorldRotation(xform);
            var offset = ent.Comp.Offset;

            var spawns = _entityTable.GetSpawns(ent.Comp.Table);
            foreach (var proto in spawns)
            {
                var vOffset = _robustRandom.NextVector2(-offset, offset);
                var trueCoords = coords.Offset(vOffset);

                Spawn(proto, trueCoords, rotation: rotation);
            }
        }
    }
}
