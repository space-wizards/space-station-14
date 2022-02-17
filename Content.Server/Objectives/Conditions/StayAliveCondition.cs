using Content.Server.Objectives.Interfaces;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed class StayAliveCondition : IObjectiveCondition
    {
        private Mind.Mind? _mind;

        public IObjectiveCondition GetAssigned(Mind.Mind mind)
        {
            return new StayAliveCondition {_mind = mind};
        }

        public string Title => Loc.GetString("objective-condition-stay-alive-title");

        public string Description => Loc.GetString("objective-condition-stay-alive-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Misc/bureaucracy.rsi"), "folder-white");

        public float Progress => (_mind?.CharacterDeadIC ?? false) ? 0f : 1f;

        public float Difficulty => 1.25f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is StayAliveCondition sac && Equals(_mind, sac._mind);
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((StayAliveCondition) obj);
        }

        public override int GetHashCode()
        {
            return (_mind != null ? _mind.GetHashCode() : 0);
        }
    }
}
