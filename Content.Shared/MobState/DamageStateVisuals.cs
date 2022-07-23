using Robust.Shared.Serialization;

namespace Content.Shared.MobState
{
    [Serializable, NetSerializable]
    public enum DamageStateVisuals : byte
    {
        State
    }

    /// <summary>
    ///     Defines what state an <see cref="Robust.Shared.GameObjects.EntityUid"/> is in.
    ///
    ///     Ordered from most alive to least alive.
    ///     To enumerate them in this way see
    ///     <see cref="DamageStateHelpers.AliveToDead"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public enum DamageState : byte
    {
        Invalid = 0,
        Alive = 1,
        Critical = 2,
        Dead = 3
    }
}
