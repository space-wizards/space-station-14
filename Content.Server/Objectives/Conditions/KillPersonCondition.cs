using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Shuttles.Systems;
using Content.Shared.CCVar;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Configuration;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public abstract class KillPersonCondition : IObjectiveCondition
    {
        protected IEntityManager EntityManager => IoCManager.Resolve<IEntityManager>();
        protected MobStateSystem MobStateSystem => EntityManager.EntitySysManager.GetEntitySystem<MobStateSystem>();
        protected Mind.Mind? Target;
        public abstract IObjectiveCondition GetAssigned(Mind.Mind mind);

        /// <summary>
        /// Whether the target must be truly dead, ignores missing evac.
        /// </summary>
        protected bool RequireDead = false;

        public string Title
        {
            get
            {
                var targetName = string.Empty;
                var jobName = Target?.CurrentJob?.Name ?? "Unknown";

                if (Target == null)
                    return Loc.GetString("objective-condition-kill-person-title", ("targetName", targetName), ("job", jobName));

                if (Target.OwnedEntity is {Valid: true} owned)
                    targetName = EntityManager.GetComponent<MetaDataComponent>(owned).EntityName;

                return Loc.GetString("objective-condition-kill-person-title", ("targetName", targetName), ("job", jobName));
            }
        }

        public string Description => Loc.GetString("objective-condition-kill-person-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Objects/Weapons/Guns/Pistols/viper.rsi"), "icon");

        public float Progress
        {
            get
            {
                if (Target == null || Target.OwnedEntity == null)
                    return 1f;

                var entMan = IoCManager.Resolve<EntityManager>();
                var mindSystem = entMan.System<MindSystem>();
                if (mindSystem.IsCharacterDeadIc(Target))
                    return 1f;

                if (RequireDead)
                    return 0f;

                // if evac is disabled then they really do have to be dead
                var configMan = IoCManager.Resolve<IConfigurationManager>();
                if (!configMan.GetCVar(CCVars.EmergencyShuttleEnabled))
                    return 0f;

                // target is escaping so you fail
                var emergencyShuttle = entMan.System<EmergencyShuttleSystem>();
                if (emergencyShuttle.IsTargetEscaping(Target.OwnedEntity.Value))
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
            return other is KillPersonCondition kpc && Equals(Target, kpc.Target);
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
            return Target?.GetHashCode() ?? 0;
        }
    }
}
