using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles.Jobs;
using Content.Shared.Roles;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;

namespace Content.Server.Objectives.Requirements
{
    [DataDefinition]
    public sealed partial class NotRoleRequirement : IObjectiveRequirement
    {
        [DataField("roleId", customTypeSerializer:typeof(PrototypeIdSerializer<JobPrototype>), required:true)]
        private string _roleId = default!;

        /// <summary>
        /// This requirement is met if the traitor is NOT the roleId, and fails if they are.
        /// </summary>
        public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
        {
            // TODO ECS this shit i keep seeing shitcode everywhere
            var entities = IoCManager.Resolve<IEntityManager>();
            if (!entities.TryGetComponent(mindId, out JobComponent? job))
                return true;

            return job.PrototypeId != _roleId;
        }
    }
}
