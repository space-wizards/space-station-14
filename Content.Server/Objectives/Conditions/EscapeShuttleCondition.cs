using Content.Server.Cuffs.Components;
using Content.Server.Objectives.Interfaces;
using Content.Server.Station.Systems;
using Content.Server.Station.Components;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class EscapeShuttleCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;
        private StationDataComponent? _stationData;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var entSysMan = IoCManager.Resolve<IEntitySystemManager>();
            var stationSystem = entSysMan.GetEntitySystem<StationSystem>();
            var stationUid = mind.OwnedEntity.HasValue ? stationSystem.GetOwningStation(mind.OwnedEntity.Value) : null;

            // Per the description of GetOwningStation, it doesn't store who
            // belongs where; it only tells us where it is currently located,
            // so we'll store that for later.
            if (!entMan.TryGetComponent<StationDataComponent>(stationUid, out var stationData))
                Logger.WarningS("objective", $"Unabled to get valid StationData for {mind}'s EscapeShuttleCondition. Will use fallback for Progress.");

            return new EscapeShuttleCondition {
                _mind = mind,
                _stationData = stationData,
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

            if (!entMan.TryGetComponent<IMapGridComponent>(shuttle, out var shuttleGrid))
                return false;

            return shuttleGrid.Grid.WorldAABB.Contains(agentXform.WorldPosition);
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

                if (_stationData == null) {
                    // Fallback and let the agent escape on any emergency shuttle.
                    foreach (var fallbackStationData in entMan.EntityQuery<StationDataComponent>())
                    {
                        if (IsAgentOnShuttle(xform, fallbackStationData.EmergencyShuttle)) {
                            shuttleContainsAgent = true;
                            break;
                        }
                    }
                } else
                    shuttleContainsAgent = IsAgentOnShuttle(xform, _stationData.EmergencyShuttle);

                return (shuttleContainsAgent && agentIsAlive && agentIsEscaping) ? 1f : 0f;
            }
        }

        public float Difficulty => 1.3f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is EscapeShuttleCondition esc && Equals(_mind, esc._mind) && Equals(_stationData, esc._stationData);
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
            return HashCode.Combine(
                (_mind != null ? _mind.GetHashCode() : 0),
                (_stationData != null ? _stationData.GetHashCode() : 0));
        }
    }
}
