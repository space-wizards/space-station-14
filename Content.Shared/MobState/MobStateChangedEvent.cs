using Content.Shared.MobState.Components;

namespace Content.Shared.MobState
{
    // public sealed class MobStateChangedEvent : EntityEventArgs
    // {
    //     public MobStateChangedEvent(
    //         MobStateComponent component,
    //         MobState? oldMobState,
    //         MobState currentMobState,
    //         EntityUid? origin)
    //     {
    //         Component = component;
    //         OldMobState = oldMobState;
    //         CurrentMobState = currentMobState;
    //         Origin = origin;
    //     }
    //     public MobStateComponent Component { get; }
    //
    //     public MobState? OldMobState { get; }
    //
    //     public MobState CurrentMobState { get; }
    //
    //     public EntityUid? Origin { get; }
    // }

    [ByRefEvent]
    public readonly record struct MobStateChangedEvent(MobStateComponent Component,
        MobState OldMobState,
        MobState CurrentMobState,
        EntityUid? Origin = null)
    {
        public EntityUid Entity => Component.Owner;
    }
    public static class A
    {
    }
}
