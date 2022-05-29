using Content.Shared.Markers;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Server.Spawners.Components
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSpawnPointComponent))]
    public sealed class SpawnPointComponent : SharedSpawnPointComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("job_id")]
        private string? _jobId;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("spawn_type")]
        public SpawnPointType SpawnType { get; } = SpawnPointType.Unset;

        public JobPrototype? Job => string.IsNullOrEmpty(_jobId) ? null : _prototypeManager.Index<JobPrototype>(_jobId);
    }

    public enum SpawnPointType
    {
        Unset = 0,
        LateJoin,
        Job,
        Observer,
    }
}
