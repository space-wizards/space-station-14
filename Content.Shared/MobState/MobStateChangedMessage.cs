using Content.Shared.MobState.State;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState
{
#pragma warning disable 618
    public class MobStateChangedMessage : ComponentMessage
#pragma warning restore 618
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
