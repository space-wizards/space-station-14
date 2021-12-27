using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Holiday.Greet;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;
using Robust.Shared.ViewVariables;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    public class ConditionalSpawnerComponent : Component, IMapInit
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "ConditionalSpawner";

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototypes", customTypeSerializer:typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> Prototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gameRules", customTypeSerializer:typeof(PrototypeIdListSerializer<GameRulePrototype>))]
        private readonly List<string> _gameRules = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chance")]
        public float Chance { get; set; } = 1.0f;

        public void RuleAdded(GameRuleAddedEvent obj)
        {
            if(_gameRules.Contains(obj.Rule.ID))
                Spawn();
        }

        private void TrySpawn()
        {
            if (_gameRules.Count == 0)
            {
                Spawn();
                return;
            }

            foreach (var rule in _gameRules)
            {
                if (!EntitySystem.Get<GameTicker>().HasGameRule(rule)) continue;
                Spawn();
                return;
            }
        }

        public virtual void Spawn()
        {
            if (Chance != 1.0f && !_robustRandom.Prob(Chance))
                return;

            if (Prototypes.Count == 0)
            {
                Logger.Warning($"Prototype list in ConditionalSpawnComponent is empty! Entity: {Owner}");
                return;
            }

            if(!_entMan.Deleted(Owner))
                _entMan.SpawnEntity(_robustRandom.Pick(Prototypes), _entMan.GetComponent<TransformComponent>(Owner).Coordinates);
        }

        public virtual void MapInit()
        {
            TrySpawn();
        }
    }
}
