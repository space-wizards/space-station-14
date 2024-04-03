using System.Threading;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.List;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    public sealed partial class TimedSpawnerComponent : Component, ISerializationHooks
    {
        [DataField(customTypeSerializer: typeof(PrototypeIdListSerializer<EntityPrototype>))]
        public List<string> Prototypes { get; set; } = new();

        public float Chance { get; set; } = 1.0f;

        public int IntervalSeconds { get; set; } = 60;

        public int MinimumEntitiesSpawned { get; set; } = 1;

        public int MaximumEntitiesSpawned { get; set; } = 1;

        public CancellationTokenSource? TokenSource;

        void ISerializationHooks.AfterDeserialization()
        {
            if (MinimumEntitiesSpawned > MaximumEntitiesSpawned)
                throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
        }
    }
}
