using Content.Server.Cuffs.Components;
using Content.Server.Objectives.Interfaces;
using Content.Server.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class EscapeShuttleCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new EscapeShuttleCondition {
                _mind = mind,
            };
        }

        public string Title => Loc.GetString("objective-condition-escape-shuttle-title");

        public string Description => Loc.GetString("objective-condition-escape-shuttle-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Structures/Furniture/chairs.rsi"), "shuttle");

        private bool IsAgentOnShuttle(TransformComponent agentXform, EntityUid? shuttle)
        {
            if (shuttle == null)
                return false;

            var entMan = IoCManager.Resolve<IEntityManager>();

            if (!entMan.TryGetComponent<MapGridComponent>(shuttle, out var shuttleGrid) ||
                !entMan.TryGetComponent<TransformComponent>(shuttle, out var shuttleXform))
            {
                return false;
            }

            return shuttleXform.WorldMatrix.TransformBox(shuttleGrid.LocalAABB).Contains(agentXform.WorldPosition);
        }

        public float Progress
        {
            get {
                var entMan = IoCManager.Resolve<IEntityManager>();

                if (_mind?.OwnedEntity == null
                    || !entMan.TryGetComponent<TransformComponent>(_mind.OwnedEntity, out var xform))
                    return 0f;

                var shuttleContainsAgent = false;
                var agentIsAlive = !_mind.CharacterDeadIC;
                var agentIsEscaping = true;

                if (entMan.TryGetComponent<CuffableComponent>(_mind.OwnedEntity, out var cuffed)
                    && cuffed.CuffedHandCount > 0)
                    // You're not escaping if you're restrained!
                    agentIsEscaping = false;

                // Any emergency shuttle counts for this objective.
                foreach (var stationData in entMan.EntityQuery<StationDataComponent>())
                {
                    if (IsAgentOnShuttle(xform, stationData.EmergencyShuttle)) {
                        shuttleContainsAgent = true;
                        break;
                    }
                }

                return (shuttleContainsAgent && agentIsAlive && agentIsEscaping) ? 1f : 0f;
            }
        }

        public float Difficulty => 1.3f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is EscapeShuttleCondition esc && Equals(_mind, esc._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((EscapeShuttleCondition) obj);
        }

        public override int GetHashCode()
        {
            return _mind != null ? _mind.GetHashCode() : 0;
        }
    }
}
