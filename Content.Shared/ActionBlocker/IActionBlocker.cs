using System;
using Content.Shared.EffectBlocker;

namespace Content.Shared.ActionBlocker
{
    /// <summary>
    /// This interface gives components the ability to block certain actions from
    /// being done by the owning entity. For effects see <see cref="IEffectBlocker"/>
    /// </summary>
    [Obsolete("Use events instead")]
    public interface IActionBlocker
    {
        [Obsolete("Use DropAttemptEvent instead")]
        bool CanDrop() => true;

        [Obsolete("Use PickupAttemptEvent instead")]
        bool CanPickup() => true;

        [Obsolete("Use AttackAttemptEvent instead")]
        bool CanAttack() => true;
    }
}
