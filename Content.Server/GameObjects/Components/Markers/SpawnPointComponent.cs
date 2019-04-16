using System;
using Content.Shared.GameObjects.Components.Markers;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Markers
{
    public sealed class SpawnPointComponent : SharedSpawnPointComponent
    {
        private SpawnPointType _spawnType;
        [ViewVariables]
        public SpawnPointType SpawnType => _spawnType;

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spawnType, "spawn_type", SpawnPointType.Unset);
        }
    }

    public enum SpawnPointType
    {
        Unset = 0,
        LateJoin,
    }
}
