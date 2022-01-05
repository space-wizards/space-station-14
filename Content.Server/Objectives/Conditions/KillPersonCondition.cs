using Content.Server.Objectives.Interfaces;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public abstract class KillPersonCondition : IObjectiveCondition
    {
        protected Mind.Mind? Target;
        public abstract IObjectiveCondition GetAssigned(Mind.Mind mind);

        public string Title
        {
            get
            {
                var targetName = string.Empty;

                if (Target == null)
                    return Loc.GetString("objective-condition-kill-person-title", ("targetName", targetName));

                if (Target.CharacterName != null)
                    targetName = Target.CharacterName;
                else if (Target.OwnedEntity is {Valid: true} owned)
                    targetName = IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(owned).EntityName;

                return Loc.GetString("objective-condition-kill-person-title", ("targetName", targetName));
            }
        }

        public string Description => Loc.GetString("objective-condition-kill-person-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Guns/Pistols/mk58_wood.rsi"), "icon");

        public float Progress => (Target?.CharacterDeadIC ?? true) ? 1f : 0f;

        public float Difficulty => 2.25f;

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
