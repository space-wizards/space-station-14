#nullable enable
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public abstract class KillPersonCondition : IObjectiveCondition
    {
        protected Mind? Target;
        public abstract IObjectiveCondition GetAssigned(Mind mind);

        public string Title => $"Kill {Target?.OwnedEntity.Name}";

        public string Description => $"Do it however you like, just make sure they don't last the shift.";

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Guns/Pistols/mk58_wood.rsi"), "icon");

        public float Progress => Target?.OwnedEntity != null &&
                                 Target.OwnedEntity
                                     .TryGetComponent<IDamageableComponent>(out var damageableComponent) &&
                                 damageableComponent.CurrentState == DamageState.Dead
                                    ? 1f
                                    : 0f;

        public float Difficulty => 2f;
    }
}
