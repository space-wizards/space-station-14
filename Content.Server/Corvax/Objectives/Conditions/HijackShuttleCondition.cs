using Content.Server.Cuffs.Components;
using Content.Server.Mind.Components;
using Content.Server.Objectives.Interfaces;
using Content.Server.Station.Components;
using Content.Server.Traitor;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [DataDefinition]
    public sealed class HijackShuttleCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new HijackShuttleCondition
            {
                _mind = mind,
            };
        }

        public string Title => Loc.GetString("objective-condition-hijack-shuttle-title");

        public string Description => Loc.GetString("objective-condition-hijack-shuttle-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Tools/emag.rsi"), "icon");

        private bool IsShuttleHijacked(TransformComponent agentXform, EntityUid? shuttle)
        {
            if (shuttle == null)
                return false;

            var entMan = IoCManager.Resolve<IEntityManager>();
            var lookupSys = IoCManager.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();

            if (!entMan.TryGetComponent<MapGridComponent>(shuttle, out var shuttleGrid) ||
                !entMan.TryGetComponent<TransformComponent>(shuttle, out var shuttleXform))
            {
                return false;
            }

            var shuttleAabb = shuttleXform.WorldMatrix.TransformBox(shuttleGrid.LocalAABB);
            var agentOnShuttle = shuttleAabb.Contains(agentXform.WorldPosition);
            var entities = lookupSys.GetEntitiesIntersecting(shuttleXform.MapID, shuttleAabb);
            foreach (var entity in entities)
            {
                if (!entMan.TryGetComponent<MindComponent>(entity, out var mind) || mind.Mind == null)
                    continue;

                var isPersonTraitor = mind.Mind.HasRole<TraitorRole>();
                if (!isPersonTraitor)
                {
                    var isPersonCuffed =
                        entMan.TryGetComponent<CuffableComponent>(mind.Mind.OwnedEntity, out var cuffed)
                        && cuffed.CuffedHandCount == 0;
                    if (isPersonCuffed)
                        return false; // Fail if some crew not cuffed
                }
            }
            // TODO: Allow pets?

            return agentOnShuttle;
        }

        public float Progress
        {
            get {
                var entMan = IoCManager.Resolve<IEntityManager>();

                if (_mind?.OwnedEntity == null
                    || !entMan.TryGetComponent<TransformComponent>(_mind.OwnedEntity, out var xform))
                    return 0f;

                var shuttleHijacked = false;
                var agentIsAlive = !_mind.CharacterDeadIC;
                var agentIsFree = true;

                if (entMan.TryGetComponent<CuffableComponent>(_mind.OwnedEntity, out var cuffed)
                    && cuffed.CuffedHandCount > 0)
                    // You're not escaping if you're restrained!
                    agentIsFree = false;

                // Any emergency shuttle counts for this objective.
                foreach (var stationData in entMan.EntityQuery<StationDataComponent>())
                {
                    if (IsShuttleHijacked(xform, stationData.EmergencyShuttle)) {
                        shuttleHijacked = true;
                        break;
                    }
                }

                return (shuttleHijacked && agentIsAlive && agentIsFree) ? 1f : 0f;
            }
        }

        public float Difficulty => 1.3f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is HijackShuttleCondition esc && Equals(_mind, esc._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((HijackShuttleCondition) obj);
        }

        public override int GetHashCode()
        {
            return _mind != null ? _mind.GetHashCode() : 0;
        }
    }
}
