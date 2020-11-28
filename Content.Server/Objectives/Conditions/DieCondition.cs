using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    public class DieCondition : IObjectiveCondition
    {
        public void ExposeData(ObjectSerializer serializer){}

        public string GetTitle() => "Die a glorius death";

        public string GetDescription() => "Die.";

        public SpriteSpecifier GetIcon() => new SpriteSpecifier.Rsi(new ResourcePath("Mobs/Ghosts/ghost_human.rsi"), "icon");

        public float GetProgress(Mind mind)
        {
            return mind.OwnedEntity != null &&
                   mind.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageableComponent) &&
                   damageableComponent.CurrentState == DamageState.Dead
                ? 0f
                : 1f;
        }

        public float GetDifficulty() => 1f;
    }
}
