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

/// <summary>
/// Event that is raised whenever a MobState changes on an entity
/// </summary>
/// <param name="Target">The Entity whose MobState is changing</param>
/// <param name="Component">The MobState Component owned by the Target entity</param>
/// <param name="OldMobState">The previous MobState</param>
/// <param name="NewMobState">The new MobState</param>
/// <param name="Origin">The Entity that caused this state change</param>
public record struct MobStateChangedEvent(EntityUid Target, MobStateComponent Component, MobState OldMobState,
    MobState NewMobState, EntityUid? Origin = null);

public static class A
{
    //^.^
}

//This is dumb and I hate it but I don't feel like refactoring this garbage
[Serializable, NetSerializable]
public enum MobStateVisuals : byte
{
    State
}
