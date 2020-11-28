#nullable enable
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
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

        public string Title => "Stay alive.";

        public string Description => "Survive this shift, we need you for another assignment.";

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Misc/skub.rsi"), "icon"); //didn't know what else would have been a good icon for staying alive

        public float Progress => _mind?.OwnedEntity != null &&
                                 _mind.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageableComponent) &&
                                 damageableComponent.CurrentState == DamageState.Dead
                                    ? 1f
                                    : 0f;

        public float Difficulty => 1f;
    }
}
