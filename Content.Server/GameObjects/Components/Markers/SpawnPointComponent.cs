using Content.Shared.GameObjects.Components.Markers;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSpawnPointComponent))]
    public sealed class SpawnPointComponent : SharedSpawnPointComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        private SpawnPointType _spawnType;
        [ViewVariables(VVAccess.ReadWrite)]
        private string _jobId;
        public SpawnPointType SpawnType => _spawnType;
        public JobPrototype Job => string.IsNullOrEmpty(_jobId) ? null
            : _prototypeManager.Index<JobPrototype>(_jobId);

        public override void ExposeData(ObjectSerializer serializer)
        {
            base.ExposeData(serializer);

            serializer.DataField(ref _spawnType, "spawn_type", SpawnPointType.Unset);
            serializer.DataField(ref _jobId, "job_id", null);
        }
    }

    public enum SpawnPointType
    {
        Unset = 0,
        LateJoin,
        Job,
        Observer,
    }
}
