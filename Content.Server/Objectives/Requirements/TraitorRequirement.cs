using Content.Server.Roles;
using Content.Shared.Mind;
using Content.Shared.Objectives.Interfaces;
using Content.Shared.Roles;
using JetBrains.Annotations;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class TraitorRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(EntityUid mindId, MindComponent mind)
        {
            var roleSystem = IoCManager.Resolve<IEntityManager>().System<SharedRoleSystem>();
            return roleSystem.MindHasRole<TraitorRoleComponent>(mindId);
        }
    }
}
