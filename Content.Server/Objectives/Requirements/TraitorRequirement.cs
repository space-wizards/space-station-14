using Content.Server.Mind.Systems;
using Content.Server.Objectives.Interfaces;
using Content.Server.Traitor;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Content.Server.Objectives.Requirements
{
    [UsedImplicitly]
    [DataDefinition]
    public class TraitorRequirement : IObjectiveRequirement
    {
        public bool CanBeAssigned(Mind.Mind mind)
        {
            var roleSys = EntitySystem.Get<RolesSystem>();
            return roleSys.HasRole<TraitorRole>(mind);
        }
    }
}
