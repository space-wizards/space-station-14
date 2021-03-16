using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager.Attributes;
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
        [DataField("prototypes")]
        public List<string> Prototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("gameRules")]
        private readonly List<string> _gameRules = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chance")]
        public float Chance { get; set; } = 1.0f;

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

            foreach (var rule in _gameRules)
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
