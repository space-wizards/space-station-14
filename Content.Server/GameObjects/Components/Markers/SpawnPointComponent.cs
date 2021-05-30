using Content.Shared.GameObjects.Components.Markers;
using Content.Shared.Roles;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.ViewVariables;

namespace Content.Server.GameObjects.Components.Markers
{
    [RegisterComponent]
    [ComponentReference(typeof(SharedSpawnPointComponent))]
    public sealed class SpawnPointComponent : SharedSpawnPointComponent
    {
        [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

        [ViewVariables(VVAccess.ReadWrite)]
        [DataField("job_id")]
        private string? _jobId;

        [field: ViewVariables(VVAccess.ReadWrite)]
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
