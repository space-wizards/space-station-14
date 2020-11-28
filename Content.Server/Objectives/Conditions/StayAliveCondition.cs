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
        public void ExposeData(ObjectSerializer serializer) {}

        public string GetTitle() => "Stay alive.";

        public string GetDescription() => "Survive this shift, we need you for another assignment.";

        public SpriteSpecifier GetIcon() => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Misc/skub.rsi"), "icon"); //didn't know what else would have been a good icon for staying alive

        public float GetProgress(Mind mind)
        {
            return mind.OwnedEntity != null &&
                   mind.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageableComponent) &&
                   damageableComponent.CurrentState == DamageState.Dead
                ? 1f
                : 0f;
        }

        public float GetDifficulty() => 1f;
    }
}
