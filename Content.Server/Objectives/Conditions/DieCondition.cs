#nullable enable
using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.GameObjects.Components.Damage;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    [UsedImplicitly]
    public class DieCondition : IObjectiveCondition
    {
        private Mind? mind;

        public IObjectiveCondition GetAssigned(Mind mind)
        {
            return new DieCondition {mind = mind};
        }

        public string Title => "Die a glorius death";

        public string Description => "Die.";

        public SpriteSpecifier Icon => new SpriteSpecifier.Rsi(new ResourcePath("Mobs/Ghosts/ghost_human.rsi"), "icon");

        public float Progress => mind?.OwnedEntity != null &&
                                 mind.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageableComponent) &&
                                    damageableComponent.CurrentState == DamageState.Dead
                                    ? 1f
                                    : 0f;

        public float Difficulty => 1f;
    }
}
