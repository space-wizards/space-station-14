#nullable enable
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Shared.Localization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    public class StayAliveCondition : IObjectiveCondition
    {
        private Mind? _mind;

        public IObjectiveCondition GetAssigned(Mind mind)
        {
            return new StayAliveCondition {_mind = mind};
        }

        public string Title => Loc.GetString("Stay alive.");

        public string Description => Loc.GetString("Survive this shift, we need you for another assignment.");

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Misc/skub.rsi"), "icon"); //didn't know what else would have been a good icon for staying alive

        public float Progress => _mind?.OwnedEntity != null &&
                                 _mind.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageableComponent) &&
                                 damageableComponent.CurrentState == DamageState.Dead
                                    ? 0f
                                    : 1f;

        public float Difficulty => 1f;

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
