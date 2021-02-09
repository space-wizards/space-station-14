using System;
using System.Collections.Generic;
using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Timers;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Random;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Prototypes.DataClasses.Attributes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;
using Timer = Robust.Shared.Timers.Timer;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    [DataClass(typeof(TimedSpawnerComponentData))]
    public class TimedSpawnerComponent : Component
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "TimedSpawner";

        [ViewVariables(VVAccess.ReadWrite)]
        [YamlField("prototypes")]
        public List<string> Prototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [YamlField("chance")]
        public float Chance { get; set; } = 1.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        [YamlField("intervalSeconds")]
        public int IntervalSeconds { get; set; } = 60;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataClassTarget("MinimumEntitiesSpawned")]
        public int MinimumEntitiesSpawned { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataClassTarget("MaximumEntitiesSpawned")]
        public int MaximumEntitiesSpawned { get; set; } = 1;

        private CancellationTokenSource TokenSource;

        public override void Initialize()
        {
            base.Initialize();
            SetupTimer();
        }

        protected override void Shutdown()
        {
            base.Shutdown();
            TokenSource.Cancel();
        }

        private void SetupTimer()
        {
            TokenSource?.Cancel();
            TokenSource = new CancellationTokenSource();
            Owner.SpawnRepeatingTimer(TimeSpan.FromSeconds(IntervalSeconds), OnTimerFired, TokenSource.Token);
        }

        private void OnTimerFired()
        {
            if (!_robustRandom.Prob(Chance))
                return;

            var number = _robustRandom.Next(MinimumEntitiesSpawned, MaximumEntitiesSpawned);

            for (int i = 0; i < number; i++)
            {
                var entity = _robustRandom.Pick(Prototypes);
                Owner.EntityManager.SpawnEntity(entity, Owner.Transform.Coordinates);
            }
        }
    }
}
