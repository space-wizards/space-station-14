using Robust.Shared.Interfaces.GameObjects;

namespace Content.Shared.GameObjects.Components.Damage
{
    /// <summary>
    ///     Defines what state an <see cref="IEntity"/> with a
    ///     <see cref="IDamageableComponent"/> is in.
    ///     Not all states must be supported - for instance, the
    ///     <see cref="IDamageableComponent"/> only supports
    ///     <see cref="DamageState.Alive"/> and <see cref="DamageState.Dead"/>,
    ///     as inanimate objects don't go into crit.
    /// </summary>
    public enum DamageState
    {
        Alive,
        Critical,
        Dead
    }
}
