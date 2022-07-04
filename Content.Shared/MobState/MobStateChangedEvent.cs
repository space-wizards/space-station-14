using Content.Shared.MobState.Components;
using Content.Shared.MobState.State;

namespace Content.Shared.MobState
{
    public sealed class MobStateChangedEvent : EntityEventArgs
    {
        public MobStateChangedEvent(
            MobStateComponent component,
            DamageState? oldMobState,
            DamageState currentMobState)
        {
            Component = component;
            OldMobState = oldMobState;
            CurrentMobState = currentMobState;
        }

        public EntityUid Entity => Component.Owner;

        public MobStateComponent Component { get; }

        public DamageState? OldMobState { get; }

        public DamageState CurrentMobState { get; }
    }
}
