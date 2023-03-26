using Content.Server.Objectives.Interfaces;
using Content.Shared.Mobs.Systems;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public abstract class KillPersonCondition : IObjectiveCondition
    {
        protected IEntityManager EntityManager => IoCManager.Resolve<IEntityManager>();
        protected MobStateSystem MobStateSystem => EntityManager.EntitySysManager.GetEntitySystem<MobStateSystem>();
        protected Mind.Mind? Target;
        public abstract IObjectiveCondition GetAssigned(Mind.Mind mind);

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

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Guns/Pistols/viper.rsi"), "icon");

        public float Progress => (Target?.CharacterDeadIC ?? true) ? 1f : 0f;

        public float Difficulty => 2f;

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
