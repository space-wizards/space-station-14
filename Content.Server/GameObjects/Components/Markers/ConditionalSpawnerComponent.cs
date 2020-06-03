using System;
using System.Collections.Generic;
using Content.Server.GameTicking;
using Content.Server.Interfaces.GameTicking;
using NFluidsynth;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using SQLitePCL;
using Logger = Robust.Shared.Log.Logger;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    public class ConditionalSpawnerComponent : Component, IMapInit
    {
        public override string Name => "ConditionalSpawner";

#pragma warning disable 649
        [Dependency] private IGameTicker _gameTicker;
        [Dependency] private IReflectionManager _reflectionManager;
        [Dependency] private IEntityManager _entityManager;
        [Dependency] private IRobustRandom _robustRandom;
#pragma warning restore 649

        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> Prototypes { get; set; } = new List<string>();

        [ViewVariables(VVAccess.ReadWrite)]
        private List<string> _gameRules = new List<string>();

        [ViewVariables(VVAccess.ReadWrite)]
        public float Chance { get; set; } = 1.0f;

        public List<Type> GameRules
        {
            get
            {
                var list = new List<Type>();
                
                foreach (var rule in _gameRules)
                {
                    list.Add(_reflectionManager.GetType(rule));
                }

                return list;
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
            if(_gameRules.Count > 0)
                TrySpawn();
        }

        private void TrySpawn()
        {
            if (_gameRules.Count == 0)
            {
                // ReSharper disable twice CompareOfFloatsByEqualityOperator
                if(Chance == 1.0f)
                    return;

                Spawn();
                return;
            }

            foreach (var rule in GameRules)
            {
                if (!_gameTicker.HasGameRule(rule)) continue;
                Spawn();
                return;
            }

            return;
        }

        private void Spawn()
        {
            if (Chance != 1.0f && !_robustRandom.Prob(Chance))
                return;

            if (Prototypes.Count == 0)
            {
                Logger.Warning($"Prototype list in ConditionalSpawnComponent is empty! Entity: {Owner}");
                return;
            }

            _entityManager.SpawnEntity(_robustRandom.Pick(Prototypes), Owner.Transform.GridPosition);
        }

        public void MapInit()
        {
            _gameTicker.OnRuleAdded += RuleAdded;

            TrySpawn();
        }
    }
}
