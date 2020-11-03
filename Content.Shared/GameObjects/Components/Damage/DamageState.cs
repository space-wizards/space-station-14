using System;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Serialization;

namespace Content.Shared.GameObjects.Components.Damage
{
    /// <summary>
    ///     Defines what state an <see cref="IEntity"/> with a
    ///     <see cref="IDamageableComponent"/> is in.
    ///     Not all states must be supported - for instance,
    ///     inanimate objects don't go into crit.
    ///
    ///     Ordered from most alive to least alive. To enumerate them in this way
    ///     see <see cref="DamageStateHelpers.AliveToDead"/>.
    /// </summary>
    [Serializable, NetSerializable]
    public enum DamageState
    {
        Invalid = 0,
        Alive = 1,
        Critical = 2,
        Dead = 3
    }
}
