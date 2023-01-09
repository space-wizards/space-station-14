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

public sealed class MobStateChangedEvent : EntityEventArgs
{
    public MobStateChangedEvent(MobStateComponent Component,
        MobState OldMobState,
        MobState NewMobState,
        EntityUid? Origin = null)
    {
        this.Component = Component;
        this.OldMobState = OldMobState;
        this.NewMobState = NewMobState;
        this.Origin = Origin;
    }
    public EntityUid Entity => Component.Owner;
    public MobStateComponent Component { get; init; }
    public MobState OldMobState { get; init; }
    public MobState NewMobState { get; init; }
    public EntityUid? Origin { get; init; }
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
