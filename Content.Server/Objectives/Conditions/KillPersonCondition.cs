using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mind;
using Content.Shared.Mobs.Systems;
using Content.Shared.Objectives.Interfaces;
using Content.Shared.Roles.Jobs;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public abstract class KillPersonCondition : IObjectiveCondition
    {
        // TODO refactor all of this to be ecs
        protected IEntityManager EntityManager => IoCManager.Resolve<IEntityManager>();
        protected SharedMindSystem Minds => EntityManager.System<SharedMindSystem>();
        protected SharedJobSystem Jobs => EntityManager.System<SharedJobSystem>();
        protected MobStateSystem MobStateSystem => EntityManager.System<MobStateSystem>();
        protected EntityUid? TargetMindId;
        protected MindComponent? TargetMind => EntityManager.GetComponentOrNull<MindComponent>(TargetMindId);
        public abstract IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind);

        /// <summary>
        /// Whether the target must be truly dead, ignores missing evac.
        /// </summary>
        protected bool RequireDead = false;

        public string Title
        {
            get
            {
                var mind = TargetMind;
                var targetName = mind?.CharacterName ?? "Unknown";
                var jobName = Jobs.MindTryGetJobName(TargetMindId);

                if (TargetMind == null)
                    return Loc.GetString("objective-condition-kill-person-title", ("targetName", targetName), ("job", jobName));

                return Loc.GetString("objective-condition-kill-person-title", ("targetName", targetName), ("job", jobName));
            }
        }

        public string Description => Loc.GetString("objective-condition-kill-person-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Objects/Weapons/Guns/Pistols/viper.rsi"), "icon");

        public float Progress
        {
            get
            {
                if (TargetMindId == null || TargetMind?.OwnedEntity == null)
                    return 1f;

                var entMan = IoCManager.Resolve<EntityManager>();
                var mindSystem = entMan.System<SharedMindSystem>();
                if (mindSystem.IsCharacterDeadIc(TargetMind))
                    return 1f;

                if (RequireDead)
                    return 0f;

                // if evac is disabled then they really do have to be dead
                var configMan = IoCManager.Resolve<IConfigurationManager>();
                if (!configMan.GetCVar(CCVars.EmergencyShuttleEnabled))
                    return 0f;

                // target is escaping so you fail
                var emergencyShuttle = entMan.System<EmergencyShuttleSystem>();
                if (emergencyShuttle.IsTargetEscaping(TargetMind.OwnedEntity.Value))
                    return 0f;

                // evac has left without the target, greentext since the target is afk in space with a full oxygen tank and coordinates off.
                if (emergencyShuttle.ShuttlesLeft)
                    return 1f;

                // if evac is still here and target hasn't boarded, show 50% to give you an indicator that you are doing good
                return emergencyShuttle.EmergencyShuttleArrived ? 0.5f : 0f;
            }
        }

        public float Difficulty => 1.75f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is KillPersonCondition kpc && Equals(TargetMindId, kpc.TargetMindId);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KillPersonCondition) obj);
        }

        public override int GetHashCode()
        {
            return TargetMindId?.GetHashCode() ?? 0;
        }
    }
}
