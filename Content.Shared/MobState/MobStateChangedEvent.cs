using Content.Shared.MobState.Components;

namespace Content.Shared.MobState
{
    public sealed class MobStateChangedEvent : EntityEventArgs
    {
        public MobStateChangedEvent(
            MobStateComponent component,
            MobState? oldMobState,
            MobState currentMobState, 
            EntityUid? origin)
        {
            Component = component;
            OldMobState = oldMobState;
            CurrentMobState = currentMobState;
            Origin = origin;
        }

        public EntityUid Entity => Component.Owner;

        public MobStateComponent Component { get; }

        public MobState? OldMobState { get; }

        public MobState CurrentMobState { get; }

        public EntityUid? Origin { get; }
    }

    public static class A
    {
        [Obsolete("Just check for the enum value instead")]
        public static bool IsAlive(this MobState state)
        {
            return state == MobState.Alive;
        }

        [Obsolete("Just check for the enum value instead")]
        public static bool IsCritical(this MobState state)
        {
            return state == MobState.Critical;
        }

        [Obsolete("Just check for the enum value instead")]
        public static bool IsDead(this MobState state)
        {
            return state == MobState.Dead;
        }

        [Obsolete("Just check for the enum value instead")]
        public static bool IsIncapacitated(this MobState state)
        {
            return state is MobState.Dead or MobState.Critical;
        }
    }
}
