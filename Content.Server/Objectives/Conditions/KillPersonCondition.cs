#nullable enable
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.GameObjects.Components.Mobs.State;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public abstract class KillPersonCondition : IObjectiveCondition
    {
        protected Mind? Target;
        public abstract IObjectiveCondition GetAssigned(Mind mind);

        public string Title => Loc.GetString("Kill {0}", Target?.OwnedEntity?.Name ?? "");

        public string Description => Loc.GetString("Do it however you like, just make sure they don't last the shift.");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Guns/Pistols/mk58_wood.rsi"), "icon");

        public float Progress
        {
            get
            {
                // This is written explicitly so that the logic can be understood.
                // The previous form was "cleaner" but also didn't work because any failure meant the person was "not dead".
                // It may be an idea to move all this logic to Mind, as, say, "Mind.CharacterDead".
                // But it's also weird and potentially situational.
                // Specific considerations when updating this:
                //  + Does being turned into a borg (if/when implemented) count as dead?
                //  + Is being transformed into a donut 'dead'?
                //  + *Ghost roles definitely shouldn't count as alive.*
                //  + Is it necessary to have a reference to a specific 'mind iteration' to cycle when certain events happen?
                //    (If being a borg or AI counts as dead, then this is highly likely, as it's still the same Mind for practical purposes.)

                // This shouldn't be possible but assume dead just so the invalid goal doesn't do anything annoying.
                if (Target == null)
                    return 1f;
                var targetOwnedEntity = Target.OwnedEntity;
                // This can be null if they're deleted (spike / brain nom)
                if (targetOwnedEntity == null)
                    return 1f;
                var targetMobState = targetOwnedEntity.GetComponentOrNull<IMobStateComponent>();
                // This can be null if it's a brain (this happens very often)
                // Brains are the result of gibbing so should definitely count as dead
                if (targetMobState == null)
                    return 1f;
                // They might actually be alive.
                return targetMobState.IsDead() ? 1f : 0f;
            }
        }

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
