using System;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Server.GameObjects.Components.Markers
{
    public partial class TimedSpawnerComponentData
    {
        [DataClassTarget("MinimumEntitiesSpawned")]
        public int MinimumEntitiesSpawned;

        [DataClassTarget("MaximumEntitiesSpawned")]
        public int MaximumEntitiesSpawned;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(this, x => x.MinimumEntitiesSpawned, "minimumEntitiesSpawned", 1);
            serializer.DataField(this, x => x.MaximumEntitiesSpawned, "maximumEntitiesSpawned", 1);

            if(MinimumEntitiesSpawned > MaximumEntitiesSpawned)
                throw new ArgumentException("MaximumEntitiesSpawned can't be lower than MinimumEntitiesSpawned!");
        }
    }
}
