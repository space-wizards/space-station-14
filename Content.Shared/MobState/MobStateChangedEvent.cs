using Content.Shared.MobState.Components;

namespace Content.Shared.MobState;
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
