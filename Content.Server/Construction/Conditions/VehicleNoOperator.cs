using System.Collections.Generic;
using Content.Shared.Construction;
using Content.Shared.Examine;
using Content.Shared.Vehicle;
using JetBrains.Annotations;

namespace Content.Server.Construction.Conditions
{
    /// <summary>
    /// Construction condition: true only if the entity has no operator.
    /// Useful for vehicles/mechs to block actions while occupied.
    /// </summary>
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class VehicleNoOperator : IGraphCondition
    {
        public bool Condition(EntityUid uid, IEntityManager entityManager)
        {
            var vehicle = entityManager.System<VehicleSystem>();
            return !vehicle.HasOperator(uid);
        }

        public bool DoExamine(ExaminedEvent args)
        {
            return false;
        }

        public IEnumerable<ConstructionGuideEntry> GenerateGuideEntry()
        {
            yield break;
        }
    }
}


