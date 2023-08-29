using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Roles;
using Content.Server.Shuttles.Components;
using Content.Shared.Cuffs.Components;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Player;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [DataDefinition]
    public sealed partial class HijackShuttleCondition : IObjectiveCondition
    {
        private MindComponent _mind;
        private EntityUid _mindId;

        public IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
        {
            return new HijackShuttleCondition
            {
                _mind = mind,
                _mindId = mindId,
            };
        }

        public string Title => Loc.GetString("objective-condition-hijack-shuttle-title");

        public string Description => Loc.GetString("objective-condition-hijack-shuttle-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResPath("Objects/Tools/emag.rsi"), "icon");

        private bool IsShuttleHijacked(EntityUid shuttleGridId)
        {
            var entMan = IoCManager.Resolve<IEntityManager>();
            var mindSystem = entMan.System<MindSystem>();
            var roleSystem = entMan.System<RoleSystem>();
            var mobStateSystem = entMan.System<MobStateSystem>();

            var agentOnShuttle = false;
            var gridPlayers = Filter.BroadcastGrid(shuttleGridId).Recipients;
            foreach (var player in gridPlayers)
            {
                if (!player.AttachedEntity.HasValue ||
                    !mindSystem.TryGetMind(player.AttachedEntity.Value, out var mindId, out var mind))
                    continue;

                if (mindId == _mindId)
                {
                    agentOnShuttle = true;
                    continue;
                }

                var isPersonTraitor = roleSystem.MindHasRole<TraitorRoleComponent>(mindId);
                if (isPersonTraitor) // Allow traitors
                    continue;

                var isPersonIncapacitated = mobStateSystem.IsIncapacitated(player.AttachedEntity.Value);
                if (isPersonIncapacitated) // Allow dead and crit
                    continue;

                var isPersonCuffed =
                    entMan.TryGetComponent<CuffableComponent>(player.AttachedEntity.Value, out var cuffed)
                    && cuffed.CuffedHandCount > 0;
                if (isPersonCuffed) // Allow handcuffed
                    continue;

                return false;
            }
            // TODO: Allow pets?

            return agentOnShuttle;
        }

        public float Progress
        {
            get {
                var entMan = IoCManager.Resolve<IEntityManager>();
                var mindSystem = entMan.System<MindSystem>();

                var shuttleHijacked = false;
                var agentIsAlive = !mindSystem.IsCharacterDeadIc(_mind);
                var agentIsFree = !(entMan.TryGetComponent<CuffableComponent>(_mind.OwnedEntity, out var cuffed)
                                     && cuffed.CuffedHandCount > 0); // You're not escaping if you're restrained!

                // Any emergency shuttle counts for this objective.
                var query = entMan.AllEntityQueryEnumerator<StationEmergencyShuttleComponent>();
                while (query.MoveNext(out var comp))
                {
                    if (comp.EmergencyShuttle == null)
                        continue;

                    if (IsShuttleHijacked(comp.EmergencyShuttle.Value))
                    {
                        shuttleHijacked = true;
                        break;
                    }
                }

                return (shuttleHijacked && agentIsAlive && agentIsFree) ? 1f : 0f;
            }
        }

        public float Difficulty => 2.75f;

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
