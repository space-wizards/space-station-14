using Content.Shared.ActionBlocker;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.Shared.MobState.State
{
    /// <summary>
    ///     Defines the blocking effects of an associated <see cref="DamageState"/>
    ///     (i.e. Normal, Critical, Dead) and what effects to apply upon entering or
    ///     exiting the state.
    /// </summary>
    public interface IMobState
    {
        bool IsAlive();

        bool IsCritical();

        bool IsDead();

        /// <summary>
        ///     Checks if the mob is in a critical or dead state.
        ///     See <see cref="IsCritical"/> and <see cref="IsDead"/>.
        /// </summary>
        /// <returns>true if it is, false otherwise.</returns>
        bool IsIncapacitated();

        /// <summary>
        ///     Called when this state is entered.
        /// </summary>
        void EnterState(IEntity entity);

        /// <summary>
        ///     Called when this state is left for a different state.
        /// </summary>
        void ExitState(IEntity entity);

        /// <summary>
        ///     Called when this state is updated.
        /// </summary>
        void UpdateState(IEntity entity, FixedPoint2 threshold);
    }
}
