using Content.Shared.GameObjects.Components.Damage;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    public class MobStateChangedMessage : EntitySystemMessage
    {
        public MobStateChangedMessage(
            SharedMobStateComponent component,
            DamageState oldDamageState,
            DamageState currentDamageState,
            IMobState oldMobState,
            IMobState currentMobState)
        {
            Component = component;
            OldDamageState = oldDamageState;
            CurrentDamageState = currentDamageState;
            OldMobState = oldMobState;
            CurrentMobState = currentMobState;
        }

        public IEntity Entity => Component.Owner;

        public SharedMobStateComponent Component { get; }

        public DamageState OldDamageState { get; }

        public DamageState CurrentDamageState { get; }

        public IMobState OldMobState { get; }

        public IMobState CurrentMobState { get; }
    }
}
