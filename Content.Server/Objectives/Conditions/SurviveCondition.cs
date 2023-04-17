using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class SurviveCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new SurviveCondition {_mind = mind};
        }

        public string Title => Loc.GetString("objective-condition-survive-title");

        public string Description => Loc.GetString("objective-condition-survive-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Clothing/Head/Helmets/spaceninja.rsi"), "icon");

        public float Difficulty => 0.5f;

        public float Progress => (_mind?.CharacterDeadIC ?? true) ? 0f : 1f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is SurviveCondition condition && Equals(_mind, condition._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((SurviveCondition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}
