#nullable enable
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Mobs.State
{
    public class MobStateChangedMessage : ComponentMessage
    {
        public MobStateChangedMessage(
            IMobStateComponent component,
            IMobState? oldMobState,
            IMobState currentMobState)
        {
            Component = component;
            OldMobState = oldMobState;
            CurrentMobState = currentMobState;
        }

        public IEntity Entity => Component.Owner;

        public IMobStateComponent Component { get; }

        public IMobState? OldMobState { get; }

        public IMobState CurrentMobState { get; }
    }
}
