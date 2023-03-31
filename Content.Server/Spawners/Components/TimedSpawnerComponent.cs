using System.Threading;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    public sealed class TimedSpawnerComponent : Component, ISerializationHooks
    {
        [Dependency] private readonly IRobustRandom _robustRandom = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("prototypes", customTypeSerializer:typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> Prototypes { get; set; } = new();

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("chance")]
        public float Chance { get; set; } = 1.0f;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("intervalSeconds")]
        public int IntervalSeconds { get; set; } = 60;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("MinimumEntitiesSpawned")]
        public int MinimumEntitiesSpawned { get; set; } = 1;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("MaximumEntitiesSpawned")]
        public int MaximumEntitiesSpawned { get; set; } = 1;

        public CancellationTokenSource? TokenSource;

        void ISerializationHooks.AfterDeserialization()
        {
            if (MinimumEntitiesSpawned > MaximumEntitiesSpawned)
                throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
        }

        protected override void Initialize()
        {
            base.Initialize();
            SetupTimer();
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
                IoCManager.Resolve<IEntityManager>().SpawnEntity(entity, IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(Owner).Coordinates);
            }
        }
    }
}
