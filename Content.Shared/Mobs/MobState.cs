using Content.Shared.Mobs.Components;
using Robust.Shared.Serialization;

namespace Content.Shared.Mobs;
/// <summary>
///     Defines what state an <see cref="Robust.Shared.GameObjects.EntityUid"/> is in.
///
///     Ordered from most alive to least alive.
///     To enumerate them in this way see
///     <see cref="MobStateHelpers.AliveToDead"/>.
/// </summary>
[Serializable, NetSerializable]
public enum MobState : byte
{
    Invalid = 0,
    Alive = 1,
    Critical = 2,
    Dead = 3
}

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
    //^.^
}

//TODO: This is dumb and I hate it but I don't feel like refactoring this garbage
[Serializable, NetSerializable]
public enum MobStateVisuals : byte
{
    State
}
