using System;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    public class ConditionalSpawnerComponent : Component, IMapInit
    {
        [Dependency] private readonly IGameTicker _gameTicker = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "ConditionalSpawner";

        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> Prototypes { get; set; } = new List<string>();

        [ViewVariables(VVAccess.ReadWrite)]
        private List<string> _gameRules = new List<string>();

        [ViewVariables(VVAccess.ReadWrite)]
        public float Chance { get; set; } = 1.0f;

        public IEnumerable<Type> GameRules
        {
            get
            {
                foreach (var rule in _gameRules)
                {
                    yield return _reflectionManager.GetType(rule);
                }
            }
        }

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);
            serializer.DataField(this, x => Prototypes, "prototypes", new List<string>());
            serializer.DataField(this, x => Chance, "chance", 1.0f);
            serializer.DataField(this, x => _gameRules, "gameRules", new List<string>());
        }

        private void RuleAdded(GameRuleAddedEventArgs obj)
        {
            if(_gameRules.Contains(obj.GameRule.GetType().Name))
                Spawn();
        }

        private void TrySpawn()
        {
            if (_gameRules.Count == 0)
            {
                Spawn();
                return;
            }

            foreach (var rule in GameRules)
            {
                if (!_gameTicker.HasGameRule(rule)) continue;
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

            if(!Owner.Deleted)
                Owner.EntityManager.SpawnEntity(_robustRandom.Pick(Prototypes), Owner.Transform.Coordinates);
        }

        public virtual void MapInit()
        {
            _gameTicker.OnRuleAdded += RuleAdded;

            TrySpawn();
        }
    }
}
