using System;
using System.Collections.Generic;
using System.Threading;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    public class TimedSpawnerComponent : Component
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        public override string Name => "TimedSpawner";

        [ViewVariables(VVAccess.ReadWrite)]
        public List<string> Prototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        public float Chance { get; set; } = 1.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        public int IntervalSeconds { get; set; } = 60;

        [ViewVariables(VVAccess.ReadWrite)]
        public int MinimumEntitiesSpawned { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
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

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.Prototypes, "prototypes", new List<string>());
            serializer.DataField(this, x => x.Chance, "chance", 1.0f);
            serializer.DataField(this, x => x.IntervalSeconds, "intervalSeconds", 60);
            serializer.DataField(this, x => x.MinimumEntitiesSpawned, "minimumEntitiesSpawned", 1);
            serializer.DataField(this, x => x.MaximumEntitiesSpawned, "maximumEntitiesSpawned", 1);

            if(MinimumEntitiesSpawned > MaximumEntitiesSpawned)
                throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
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
