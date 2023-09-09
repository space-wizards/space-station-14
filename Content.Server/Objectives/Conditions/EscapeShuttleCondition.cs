using Content.Server.Mind;
using Content.Server.Objectives.Interfaces;
using Content.Server.Shuttles.Systems;
using Content.Shared.Cuffs.Components;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    [DataDefinition]
    public sealed partial class EscapeShuttleCondition : IObjectiveCondition
    {
        // TODO refactor all of this to be ecs
        private MindComponent? _mind;

        public IObjectiveCondition GetAssigned(EntityUid mindId, MindComponent mind)
        {
            return new EscapeShuttleCondition
            {
                _mind = mind,
            };
        }

        public string Title => Loc.GetString("objective-condition-escape-shuttle-title");

        public string Description => Loc.GetString("objective-condition-escape-shuttle-description");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ("Structures/Furniture/chairs.rsi"), "shuttle");

        public float Progress
        {
            get {
                var entMan = IoCManager.Resolve<IEntityManager>();
                var mindSystem = entMan.System<MindSystem>();

                if (_mind?.OwnedEntity == null
                    || !entMan.TryGetComponent<TransformComponent>(_mind.OwnedEntity, out var xform))
                    return 0f;

                if (mindSystem.IsCharacterDeadIc(_mind))
                    return 0f;

                if (entMan.TryGetComponent<CuffableComponent>(_mind.OwnedEntity, out var cuffed)
                    && cuffed.CuffedHandCount > 0)
                {
                    // You're not escaping if you're restrained!
                    return 0f;
                }

                // Any emergency shuttle counts for this objective, but not pods.
                var emergencyShuttle = entMan.System<EmergencyShuttleSystem>();
                if (!emergencyShuttle.IsTargetEscaping(_mind.OwnedEntity.Value))
                    return 0f;

                return 1f;
            }
        }

        public float Difficulty => 1.3f;

        public bool Equals(IObjectiveCondition? other)
        {
            return other is EscapeShuttleCondition esc && Equals(_mind, esc._mind);
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
            return _mind != null ? _mind.GetHashCode() : 0;
        }
    }
}
