using Content.Shared.MobState.Components;

namespace Content.Shared.MobState
{
    public sealed class MobStateChangedEvent : EntityEventArgs
    {
        public MobStateChangedEvent(
            MobStateComponent component,
            DamageState? oldMobState,
            DamageState currentMobState, 
            EntityUid? origin)
        {
            Component = component;
            OldMobState = oldMobState;
            CurrentMobState = currentMobState;
            Origin = origin;
        }

        public EntityUid Entity => Component.Owner;

        public MobStateComponent Component { get; }

        public DamageState? OldMobState { get; }

        public DamageState CurrentMobState { get; }

        public EntityUid? Origin { get; }
    }

    public static class A
    {
        [Obsolete("Just check for the enum value instead")]
        public static bool IsAlive(this DamageState state)
        {
            return state == DamageState.Alive;
        }

        [Obsolete("Just check for the enum value instead")]
        public static bool IsCritical(this DamageState state)
        {
            return state == DamageState.Critical;
        }

        [Obsolete("Just check for the enum value instead")]
        public static bool IsDead(this DamageState state)
        {
            return state == DamageState.Dead;
        }

        [Obsolete("Just check for the enum value instead")]
        public static bool IsIncapacitated(this DamageState state)
        {
            return state is DamageState.Dead or DamageState.Critical;
        }
    }
}
