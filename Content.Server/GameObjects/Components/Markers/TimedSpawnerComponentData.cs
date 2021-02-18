using System;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.GameObjects.Components.Markers
{
    public partial class TimedSpawnerComponentData : ISerializationHooks
    {
        [DataField("MinimumEntitiesSpawned")] [DataClassTarget("MinimumEntitiesSpawned")]
        public int MinimumEntitiesSpawned = 1;

        [DataField("MaximumEntitiesSpawned")] [DataClassTarget("MaximumEntitiesSpawned")]
        public int MaximumEntitiesSpawned = 1;

        public void AfterDeserialization()
        {
            if (MinimumEntitiesSpawned > MaximumEntitiesSpawned)
                throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
        }
    }
}
