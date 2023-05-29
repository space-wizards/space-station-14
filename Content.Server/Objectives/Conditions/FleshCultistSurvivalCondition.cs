using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class FleshCultistSurvivalCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new FleshCultistSurvivalCondition {_mind = mind};
        }

        public string Title => Loc.GetString("objective-condition-flesh-cultist-survival-title");

        public string Description => Loc.GetString("objective-condition-flesh-cultist-survival-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Texture(
            new ResPath("Interface/Actions/fleshCultistSurvivalObjective.png"));

        public float Progress => (_mind?.CharacterDeadIC != true) ? 1f : 0f;

        public float Difficulty => 0.5f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is FleshCultistSurvivalCondition condition && Equals(_mind, condition._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((DieCondition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}
