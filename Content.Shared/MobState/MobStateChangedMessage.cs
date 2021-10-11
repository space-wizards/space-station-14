using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState
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
