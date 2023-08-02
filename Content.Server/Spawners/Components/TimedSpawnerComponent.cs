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
    }
}
