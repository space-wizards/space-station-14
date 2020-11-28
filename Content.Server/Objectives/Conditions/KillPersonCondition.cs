using Content.Server.Mobs;
using Content.Server.Objectives.Interfaces;
using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Content.Server.Objectives.Conditions
{
    public abstract class KillPersonCondition : IObjectiveCondition
    {
        private Mind _target;

        protected Mind Target
        {
            get => _target;
            set
            {
                _target = value;
                _desc = _target.OwnedEntity != null ? $"Kill {_target.OwnedEntity.Name}" : "ERROR NO ENTITY ATTACHED TO MIND";
            }
        }

        public void ExposeData(ObjectSerializer serializer) {}

        private string _desc;
        public string GetTitle() => _desc;

        public string GetDescription() => $"{_desc}. Do it however you like, just make sure they don't last the shift.";

        public SpriteSpecifier GetIcon() => new SpriteSpecifier.Rsi(new ResourcePath("Objects/Weapons/Guns/Pistols/mk58_wood.rsi"), "icon");

        public float GetProgress(Mind mind)
        {
            return Target.OwnedEntity != null &&
                   Target.OwnedEntity.TryGetComponent<IDamageableComponent>(out var damageableComponent) &&
                   damageableComponent.CurrentState == DamageState.Dead
                ? 1f
                : 0f;
        }

        public float GetDifficulty() => 2f;
    }
}
